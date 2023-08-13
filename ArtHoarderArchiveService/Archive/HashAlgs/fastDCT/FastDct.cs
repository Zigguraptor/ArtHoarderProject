using ArtHoarderArchiveService.Archive.Infrastructure;

namespace ArtHoarderArchiveService.Archive.HashAlgs.fastDCT;

public class FastDct : IPerceptualHashAlgorithm
{
    private const double C0 = 0.3535533905932738; //1d / (2d * (sqrt(2))) //sqrt(2) = 1.4142135623730950d
    private const double C4 = 0.35355339059327373; // sqrt(2) / 4
    private const double A = 0.488;
    private const double B = 0.463;
    private const double C = 0.416;
    private const double D = 0.4192;
    private const double E = 0.098;
    private const double F = 0.0278;
    public string HashName => "FastDCTv1";

    public byte[] ComputeHash(double[,] image)
    {
        var dctMatrix = FastDct2d(image, out var avg);

        var bytes = new byte[8];
        for (var i = 0; i < 8; i++)
        {
            for (var j = 0; j < 8; j++)
            {
                if (dctMatrix[i, j] > avg)
                    bytes[i] = (byte)(bytes[i] | 1 << (7 - j));
            }
        }

        return bytes;
    }

    private static double[,] FastDct2d(double[,] matrix, out double avg)
    {
        avg = 0;
        var dctMatrix = new double[8, 8];
        var vector = new double[8];
        for (var i = 0; i < 8; i++)
        {
            for (var i1 = 0; i1 < 8; i1++)
                vector[i1] = matrix[i, i1];

            vector = FastDct1d(vector);
            for (var j = 0; j < 8; j++)
                dctMatrix[i, j] = vector[j];
        }

        for (var i = 0; i < 8; i++)
        {
            for (var j = 0; j < 8; j++)
                vector[j] = matrix[j, i];

            vector = FastDct1d(vector);
            for (var j = 0; j < 8; j++)
            {
                avg += vector[j];
                dctMatrix[j, i] = vector[j];
            }
        }

        avg /= 64;
        return dctMatrix;
    }

    private static double[] FastDct1d(double[] vector)
    {
        //init matrix
        var vectorA = new[]
        {
            (B - D) * (vector[0] - vector[3] - vector[4] + vector[7]),
            D * (vector[0] + vector[1] - vector[2] - vector[4] - vector[5] + vector[6] + vector[7]),
            -(B - D) * (vector[1] - vector[2] - vector[5] + vector[6])
        };

        var vectorB = new[]
        {
            (E + A - F + C) * (vector[0] - vector[7]),
            (F - C) * (vector[0] - vector[7] + vector[6] - vector[1]),
            (A - E - F + C) * (vector[6] - vector[1])
        };

        var vectorC = new[]
        {
            (-A - C) * (vector[0] - vector[7] + vector[3] - vector[4]),
            C * (vector[0] - vector[7] + vector[3] - vector[4] + vector[6] - vector[1] + vector[2] - vector[5]),
            (E - C) * (vector[6] - vector[1] + vector[2] - vector[5])
        };

        var vectorD = new[]
        {
            (E - A - F - C) * (vector[3] - vector[4]),
            (F + C) * (vector[3] - vector[4] + vector[2] - vector[5]),
            (A + E - F - C) * (vector[2] - vector[5])
        };

        //calculate sums
        var vectorSb = new[]
        {
            vectorB[0] + vectorB[1],
            vectorB[1] + vectorB[2]
        };

        var vectorSc = new[]
        {
            vectorC[0] + vectorC[1],
            vectorC[1] + vectorC[2]
        };

        var vectorSd = new[]
        {
            vectorD[0] + vectorD[1],
            vectorD[1] + vectorD[2]
        };


        var result = new double[8];

        result[0] = C0 * vector[0] + vector[1] + vector[2] + vector[3] + vector[4] + vector[5] + vector[6] + vector[7];
        result[4] = C4 * vector[0] - vector[1] - vector[2] + vector[3] + vector[4] - vector[5] - vector[6] + vector[7];

        result[2] = vectorA[0] + vectorA[1];
        result[6] = vectorA[1] + vectorA[2];

        result[7] = vectorSb[0] + vectorSc[0];
        result[5] = vectorSb[1] + vectorSc[1];
        result[1] = -(vectorSc[0] - vectorSd[0]);
        result[3] = vectorSc[1] - vectorSd[1];
        return result;
    }
}
