using System;

public enum PuzzleGrade { S, A, B, C, D }

public static class PuzzleRating
{
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
