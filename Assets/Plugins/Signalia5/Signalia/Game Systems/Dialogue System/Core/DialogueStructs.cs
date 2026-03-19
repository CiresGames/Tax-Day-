using System.Linq;
using AHAKuo.Signalia.Utilities;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.DialogueSystem
{
    /// <summary>
    /// A simple struct for retaining a reference to a dialogue object. Used when kickstarting and beginning to read dialogue.
    /// I honestly might be doing too much boilerplate for no reason. Could just read asset directly. This is subject to change.
    /// It's all internal though so I don't care right now.
    /// </summary>
    public readonly struct DialogueObjPlug
    {
        public readonly DialogueBook source, exit;
        public readonly Line[] lines;
        public readonly string startEvent, endEvent;

        public DialogueObjPlug(DialogueBook source, Line[] lines, string startEvent, string endEvent,
            DialogueBook exit)
        {
            this.source = source;
            this.lines = lines;
            this.startEvent = startEvent;
            this.endEvent = endEvent;
            this.exit = exit;
        }
        
        /// <summary>
        /// Reads a line from the lines array. Takes in an index and branch.
        /// NOTE: Index is branch-relevant, meaning, even if branch 'X' starts halfway through the array. It still starts at index 0.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="branch"></param>
        /// <returns></returns>
        public Line ReadLine(int i, string branch)
        {
            var branchArray = lines.ToList().Where(x => x.BranchName == branch).ToList(); // is this nice? Should get the branch items in order.
            
            if (branchArray.Count <= 0
                || i > branchArray.Count-1 // -1 because index starts at 0
                || branchArray[i] == null)
            {
                return null;
            }
            
            return branchArray[i];
        }
        
        /// <summary>
        /// Returns true if the index is outside the array bounds for the specific branch.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="branch"></param>
        /// <returns></returns>
        public bool OutsideIndex(int i, string branch)
        {
             var branchCount = lines.Count(x => x.BranchName == branch);
             return branchCount - 1 < i;
        }
    }
}
