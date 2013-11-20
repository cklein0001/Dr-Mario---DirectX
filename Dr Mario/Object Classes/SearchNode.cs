using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dr_Mario.Object_Classes
{
    /// <summary>
    /// Reresents one node in the search space
    /// </summary>
    public class SearchNode
    {
        /// <summary>
        /// Location on the map
        /// </summary>
        public System.Drawing.Point Position;
        /// <summary>
        /// If true, this tile can be walked on.
        /// </summary>
        public bool Walkable;

        /// <summary>
        /// This contains references to the for nodes surrounding 
        /// this tile (Up, Down, Left, Right).
        /// </summary>
        public SearchNode[] Neighbors;
        /// <summary>
        /// A reference to the node that transfered this node to
        /// the open list. This will be used to trace our path back
        /// from the goal node to the start node.
        /// </summary>
        public SearchNode Parent;

        /// <summary>
        /// Provides an easy way to check if this node
        /// is in the open list.
        /// </summary>
        public bool InOpenList;
        /// <summary>
        /// Provides an easy way to check if this node
        /// is in the closed list.
        /// </summary>
        public bool InClosedList;

        /// <summary>
        /// The approximate distance from the start node to the
        /// goal node if the path goes through this node. (F)
        /// </summary>
        public float DistanceToGoal;
        /// <summary>
        /// Distance traveled from the spawn point. (G)
        /// </summary>
        public float DistanceTraveled;
    }

}
