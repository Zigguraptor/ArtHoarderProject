#pragma once
extern "C" __declspec(dllexport) void calculate_dct_hash(double matrix[8][8], unsigned char* output);

class fast_dct
{
public:
    static double** fast_dct2d(double matrix[8][8], double* avg);

private:
    static constexpr double c0 = 0.3535533905932738; //1d / (2d * (sqrt(2))) //sqrt(2) = 1.4142135623730950d
    static constexpr double c4 = 0.35355339059327373; // sqrt(2) / 4

    static constexpr double a = 0.488;
    static constexpr double b = 0.463;
    static constexpr double c = 0.416;
    static constexpr double d = 0.4192;
    static constexpr double e = 0.098;
    static constexpr double f = 0.0278;

    static double* fast_dct_1d(double vector[8]);
};
