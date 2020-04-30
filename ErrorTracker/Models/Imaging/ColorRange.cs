using System.Drawing;

namespace ErrorTracker
{
    class ColorRange
    {
        readonly Color _colorValue;
        readonly int _matchTreshold;

        public Color ColorValue { get => _colorValue; }
        public int MatchThreshold { get => _matchTreshold; }

        public ColorRange(Color colorValue, int matchThreshold)
        {
            _colorValue = colorValue;
            _matchTreshold = matchThreshold;
        }
    }
}
