#include "FastDCT.h"

//быстро и не читабельно.
void calculate_dct_hash(double matrix[8][8], unsigned char* output)
{
    double avg = 0;
    const auto dct_matrix = fast_dct::fast_dct2d(matrix, &avg);

    for (int i = 0; i < 8; i++)
    {
        for (int j = 0; j < 8; j++)
        {
            //unflip after fast_dct2d()
            if (dct_matrix[j][i] > avg)
            {
                output[i] |= 1 << (7 - j);
            }
        }
    }
}

double** fast_dct::fast_dct2d(double matrix[8][8], double* avg)
{
    const auto dct_matrix = new double*[8];

    for (int i = 0; i < 8; ++i)
        dct_matrix[i] = new double[8];

    for (int i = 0; i < 8; ++i)
    {
        const auto dct_vector = fast_dct_1d(matrix[i]);

        //this will flip the matrix
        for (int j = 0; j < 8; ++j)
            dct_matrix[j][i] = dct_vector[j];
    }

    for (int i = 0; i < 8; ++i)
    {
        const auto dct_vector = fast_dct_1d(dct_matrix[i]);
        dct_matrix[i] = dct_vector;
        *avg += dct_vector[0] + dct_vector[1] + dct_vector[2] + dct_vector[3] + dct_vector[4] + dct_vector[5] +
            dct_vector[6] + dct_vector[7];
    }
    *avg = *avg / 64;
    return dct_matrix;
}

double* fast_dct::fast_dct_1d(double vector[8])
{
    //init matrix
    const double matrix_a[] = {
        (b - d) * (vector[0] - vector[3] - vector[4] + vector[7]),
        d * (vector[0] + vector[1] - vector[2] - vector[4] - vector[5] + vector[6] + vector[7]),
        -(b - d) * (vector[1] - vector[2] - vector[5] + vector[6])
    };
    const double matrix_b[] = {
        (e + a - f + c) * (vector[0] - vector[7]),
        (f - c) * (vector[0] - vector[7] + vector[6] - vector[1]),
        (a - e - f + c) * (vector[6] - vector[1])
    };

    const double matrix_c[] = {
        (-a - c) * (vector[0] - vector[7] + vector[3] - vector[4]),
        c * (vector[0] - vector[7] + vector[3] - vector[4] + vector[6] - vector[1] + vector[2] - vector[5]),
        (e - c) * (vector[6] - vector[1] + vector[2] - vector[5])
    };

    const double matrix_d[] = {
        (e - a - f - c) * (vector[3] - vector[4]),
        (f + c) * (vector[3] - vector[4] + vector[2] - vector[5]),
        (a + e - f - c) * (vector[2] - vector[5])
    };

    //calculate result
    const double matrix_eb[] = {
        matrix_b[0] + matrix_b[1],
        matrix_b[1] + matrix_b[2]
    };

    const double matrix_ec[] = {
        matrix_c[0] + matrix_c[1],
        matrix_c[1] + matrix_c[2]
    };

    const double matrix_ed[] = {
        matrix_d[0] + matrix_d[1],
        matrix_d[1] + matrix_d[2]
    };

    const auto result = new double[8];

    result[0] = c0 * vector[0] + vector[1] + vector[2] + vector[3] + vector[4] + vector[5] + vector[6] + vector[7];
    result[4] = c4 * vector[0] - vector[1] - vector[2] + vector[3] + vector[4] - vector[5] - vector[6] + vector[7];

    result[2] = matrix_a[0] + matrix_a[1];
    result[6] = matrix_a[1] + matrix_a[2];

    result[7] = matrix_eb[0] + matrix_ec[0];
    result[5] = matrix_eb[1] + matrix_ec[1];
    result[1] = -(matrix_ec[0] - matrix_ed[0]);
    result[3] = matrix_ec[1] - matrix_ed[1];

    return result;
}
