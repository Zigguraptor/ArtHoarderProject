using ArtHoarderCore.Infrastructure;

namespace ArtHoarderCore.HashAlgs;

// GPT generated
public class DctHash : IImageHasher
{
    public string HashName { get; } = "DCT Hash v1";

    public Byte[] ComputeHash(double[,] image)
    {
        var rows = image.GetLength(0);
        var cols = image.GetLength(1);

        // Compute the DCT coefficients.
        var dct = Dct(image);

        // Compute the average DCT coefficient (excluding the DC term).
        var sumDct = 0.0d;
        for (var i = 0; i < rows; i++)
        {
            for (var j = 0; j < cols; j++)
            {
                sumDct += dct[i, j];
            }
        }

        var avgDct = sumDct / ((double)rows * cols);

        // Compute the hash by comparing each DCT coefficient with the average.
        var hash = new ByteArrayBuilder(128);
        for (var i = 0; i < rows; i++)
        {
            for (var j = 0; j < cols; j++)
            {
                hash.Append(dct[i, j] > avgDct);
            }
        }

        return hash.ToArray();
    }

    private static double[,] Dct(double[,] matrix)
    {
        var rows = matrix.GetLength(0);
        var cols = matrix.GetLength(1);
        var dct = new double[rows, cols];

        for (var i = 0; i < rows; i++)
        {
            for (var j = 0; j < cols; j++)
            {
                double sum = 0.0d;
                for (var ii = 0; ii < rows; ii++)
                {
                    for (var jj = 0; jj < cols; jj++)
                    {
                        double alpha = (ii == 0) ? Math.Sqrt(1.0 / rows) : Math.Sqrt(2.0 / rows);
                        double beta = (jj == 0) ? Math.Sqrt(1.0 / cols) : Math.Sqrt(2.0 / cols);
                        sum += alpha * beta * matrix[ii, jj] * Math.Cos((2 * i + 1) * ii * Math.PI / (2.0 * rows)) *
                               Math.Cos((2 * j + 1) * jj * Math.PI / (2.0 * cols));
                    }
                }

                dct[i, j] = sum;
            }
        }

        return dct;
    }
}