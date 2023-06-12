public enum ScaleFactor
{
    // origin
    One,

    // 1/2
    Half,

    // 1/4
    Quarter,

    // 1/8
    Eighth
}

public static class ScaleModeExtensions
{
    public static float ToFloat(this ScaleFactor mode)
    {
        switch (mode)
        {
            case ScaleFactor.Eighth:
                return 0.125f;
            case ScaleFactor.Quarter:
                return 0.25f;
            case ScaleFactor.Half:
                return 0.5f;
        }

        return 1.0f;
    }
}