using System;
using System.Collections.Generic;

namespace BoardFlow.Editor.Data
{
    [Serializable]
    public class BoardFlowData
    {
        public int version;
        public string activeBoardId;
        public GridSettings gridSettings;
        public List<BoardData> boards;

        public BoardFlowData()
        {
            version = 1;
            activeBoardId = string.Empty;
            gridSettings = new GridSettings();
            boards = new List<BoardData>();
        }

        public BoardData GetActiveBoard()
        {
            if (string.IsNullOrEmpty(activeBoardId) || boards.Count == 0)
                return null;

            for (int i = 0; i < boards.Count; i++)
            {
                if (boards[i].id == activeBoardId)
                    return boards[i];
            }

            return null;
        }
    }
}
