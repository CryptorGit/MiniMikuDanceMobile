using System.Linq;
using Xunit;

namespace TimelineTests;

public class UnitTest1
{
    private class SelectionManager
    {
        private readonly HashSet<(int Row, int Frame)> _selection = new();
        public IEnumerable<(int Row, int Frame)> Selection => _selection;

        public void Select(int row, int frame, bool append = false)
        {
            var key = (row, frame);
            if (_selection.Contains(key))
            {
                _selection.Remove(key);
                return;
            }
            if (!append) _selection.Clear();
            _selection.Add(key);
        }
    }

    [Fact]
    public void ToggleSelectionOnRepeatedTap()
    {
        var sm = new SelectionManager();
        sm.Select(1, 2);
        Assert.Contains((1, 2), sm.Selection.ToList());
        sm.Select(1, 2);
        Assert.Empty(sm.Selection);
    }
}
