using System;

public enum PuzzleGrade { S, A, B, C, D }

public static class PuzzleRating
{
    public static int StarsFor(int moves, int minimum)
    {
        if (minimum <= 0) throw new ArgumentOutOfRangeException(nameof(minimum));
        if (moves < minimum) throw new ArgumentOutOfRangeException(nameof(moves));
        long extra = (long)moves - minimum;
        return extra * 4 <= minimum ? 3 : extra <= minimum ? 2 : 1;
    }

    // Read-only compatibility with scores saved by earlier template versions.
    public static int StarsFromLegacyGrade(int saved)
        => saved == 1 || saved == 2 ? 3 : saved == 3 || saved == 4 ? 2 : saved == 5 ? 1 : 0;

    public static PuzzleGrade GradeFor(int moves, int minimum)
    {
        if (minimum <= 0) throw new ArgumentOutOfRangeException(nameof(minimum));
        if (moves < minimum) throw new ArgumentOutOfRangeException(nameof(moves));
        long extra = (long)moves - minimum;
        if (extra == 0) return PuzzleGrade.S;
        if (extra * 4 <= minimum) return PuzzleGrade.A;
        if (extra * 2 <= minimum) return PuzzleGrade.B;
        if (extra <= minimum) return PuzzleGrade.C;
        return PuzzleGrade.D;
    }
}
