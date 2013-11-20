using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Output = System.Diagnostics.Debug;
namespace Dr_Mario.Object_Classes
{
    public class BottleCpu : Bottle
    {
        public BottleCpu(int playerIndex)
            : base(playerIndex)
        {

        }

        public override void FillBottle(int? randomSeed)
        {
            base.FillBottle(randomSeed);
            this.Clear();
        }

        private Location TargetA = null;
        private Location TargetB = null;
        private TargetType Target = TargetType.NoMatchAtAll;
        private bool TargetHasVirus = false;
        private enum TargetType { FullMatchVertical, FullMatchHorizontal, HalfMatchVertical,HalfMatchHorizontal, QuarterMatchVertical,QuarterMatchHorizontal, NoMatchVertial,NoMatchHorizontal, NoMatchAtAll }

        public override void Input(Movement command)
        {
            if (this.PlayMode == Object_Classes.PlayMode.SinglePlayer && this.CurrentCapsule != null && this.Target == TargetType.NoMatchAtAll && this.TargetA == null && this.TargetB == null)
            {
                var CA = this.CurrentCapsule.Color;
                var CB = this.CurrentCapsule.JoinedItem(this).Color;
                try
                {
                    var matches =Search();
                    GetBestMatch(matches, CA, CB);
                    //SearchByX(CA, CB);
                    if (this.Target != TargetType.FullMatchVertical)
                        SearchByY(CA, CB);
                    if (this.Target == TargetType.NoMatchAtAll)
                        SafeDrop(CA, CB);
                    //Output.WriteLine("Targets Set.");
                    if (TargetA != null && TargetB != null && TargetA.X > TargetB.X)
                    {
                        var tt = TargetA;
                        TargetA = TargetB;
                        TargetB = tt;
                    }
                    //else if (TargetA!=null && TargetB != null && TargetA.X ==TargetB.X && TargetA.Y < tar
                    else if (TargetA == null && TargetB != null)
                    {
                        TargetA = TargetB;
                        TargetB = null;
                    }

                    string output = string.Format(@"Targets:{0},{1}
Capsule:{2},{3}", TargetA, TargetB, CurrentCapsule,CurrentCapsule.JoinedItem(this));
                    System.Diagnostics.Debug.WriteLine(output);
                }
                catch (Exception eee)
                {
                    Output.WriteLine(eee);
                    Output.WriteLine(eee.StackTrace);
                }
            }
            try
            {
                if (this.CurrentCapsule != null)
                    base.Input(GetRoute());
            }
            catch (Exception eeee)
            {
                Output.WriteLine(eeee);
                Output.WriteLine(eeee.StackTrace);
            }
        }

        private Movement GetRoute()
        {
            Location J = this.CurrentCapsule.JoinedItem(this);
            bool AlignWithJ = false ;

            Movement input = Movement.None;
            if (this.TargetA != null && this.TargetB != null)
            {

                if (TargetA.X == TargetB.X && this.CurrentCapsule.X != J.X)
                {
                    input = input | Movement.Clockwise;
                }
                else if (this.TargetA.X == TargetB.X && TargetA.Y > TargetB.Y && this.CurrentCapsule.Color != TargetA.Color && this.Target != TargetType.NoMatchAtAll && this.Target != TargetType.NoMatchVertial)
                {
                    input = input | Movement.Clockwise;
                }
                else if (this.TargetA.X != TargetB.X && this.CurrentCapsule.X == J.X)
                { input = input | Movement.Clockwise; }
                else if (this.TargetA.X != TargetB.X && TargetA.X == CurrentCapsule.X && TargetA.Color != this.CurrentCapsule.Color)
                { input = input | Movement.Clockwise; }
                else if (this.TargetA.X != TargetB.X && TargetA.X == J.X && TargetA.Color != J.Color)
                { input = input | Movement.Clockwise; }

                if (this.lastMoveCommand != Movement.None && this.lastRotateCommand == (input & Movement.Clockwise))
                    input = (input ^ Movement.Clockwise) | Movement.NoRotate;

                if (this.TargetA.X < (AlignWithJ ? J.X : CurrentCapsule.X))
                {
                    input = input | Movement.Left;
                }
                else if (this.TargetA.X > (AlignWithJ ? J.X : CurrentCapsule.X))
                {
                    input = input | Movement.Right;
                }
            }
            else if (this.TargetB == null)
            {
                int left = 0, right = 0;
                #region Check alignment w/J
                {
                    if (TargetA.X < this.Width - 1)
                    {
                        for (int y = TargetA.Y; y < this.Height; y++)
                        {
                            if (!Locations[TargetA.X + 1, y].IsEmpty)
                            {
                                right = y;
                                break;
                            }

                        }
                    }
                    if (TargetA.X > 0)
                    {
                        for (int y = TargetA.Y; y < this.Height - 1; y++)
                        {
                            if (!Locations[TargetA.X - 1, y].IsEmpty)
                            {
                                left = y;
                                break;
                            }
                        }
                    }
                    // Output.WriteLine(string.Format("{0},{1}", left, right));
                    if (TargetA.X < this.Width - 1 && !Locations[TargetA.X + 1, TargetA.Y - 1].IsEmpty)
                        AlignWithJ = true;
                    else if (left < right && TargetA.X != 0 && TargetA.X != this.Width - 1)
                    {
                        AlignWithJ = true;
                    }
                    else if (TargetA.X == this.Width - 1)
                        AlignWithJ = true;
                }
                #endregion

                if (this.CurrentCapsule.X == J.X)
                {
                    input = input | Movement.Clockwise;
                }
                else if (!AlignWithJ && TargetA.X == 0 && CurrentCapsule.Color != TargetA.Color)
                    input = input | Movement.Clockwise;
                else if (AlignWithJ && TargetA.X == this.Width - 1 && J.X == this.Width - 1 && J.Color != TargetA.Color)
                    input = input | Movement.Clockwise;
                else if (TargetA.Color != (AlignWithJ ? J.Color : CurrentCapsule.Color))
                {
                    input = input | Movement.Clockwise;
                }
                else if (this.CurrentCapsule.X != J.X &&
                    left < right &&
                    TargetA.X != 0 &&
                    TargetA.X != this.Width - 1 &&
                    J.Color != TargetA.Color)
                {
                    input = input | Movement.Clockwise;
                }

                if (this.TargetA.X < (AlignWithJ ? J.X : CurrentCapsule.X))
                {
                    input = input | Movement.Left;
                }
                else if (this.TargetA.X > (AlignWithJ ? J.X : CurrentCapsule.X))
                {
                    input = input | Movement.Right;
                }
            }
            else if (this.TargetA == null)
            {
                AlignWithJ = TargetB.X == this.Width - 1;
                if (this.CurrentCapsule.X == J.X)
                {
                    input = input | Movement.Clockwise;
                }
                else if (TargetB.X == 0 && CurrentCapsule.Color != TargetB.Color)
                    input = input | Movement.Clockwise;
                else if (TargetB.X == this.Width - 1 && J.X == this.Width - 1 && J.Color != TargetB.Color)
                    input = input | Movement.Clockwise;
                else if (this.CurrentCapsule.X != J.X)
                {

                    int left = 0, right = 0;
                    if (TargetB.X < this.Width - 1)
                    {
                        for (int y = TargetB.Y; y < this.Height - 1; y++)
                        {
                            if (!Locations[TargetB.X + 1, y].IsEmpty)
                            {
                                right = y;
                                break;
                            }

                        }
                    }
                    if (TargetB.X > 0)
                    {
                        for (int y = TargetB.Y; y < this.Height - 1; y++)
                        {
                            if (!Locations[TargetB.X - 1, y].IsEmpty)
                            {
                                left = y;
                                break;
                            }

                        }
                    }
                    //Output.WriteLine(string.Format("{0},{1}", left, right));
                    if (left < right)
                    {
                        AlignWithJ = true;
                        if (J.Color != TargetB.Color)
                        {
                            input = input | Movement.Clockwise;
                        }
                    }
                }

           
                if (this.TargetB.X < (AlignWithJ ? J.X : CurrentCapsule.X))
                {
                    input = input | Movement.Left;
                }
                else if (this.TargetB.X > (AlignWithJ ? J.X : CurrentCapsule.X))
                {
                    input = input | Movement.Right;
                }
            }

            if (this.lastMoveCommand != Movement.None && this.lastRotateCommand == (input & Movement.Clockwise))
                input = (input ^ Movement.Clockwise) | Movement.NoRotate;
            
            if (input == Movement.None)
                input = Movement.Down;

            return input;
        }
        string lastOutput;
        protected override void LockCapsule(string lockSound = @".\Sounds\pill_land.wav")
        {
            try
            {
                if (CurrentCapsule != null)
                {
                    string output = string.Format(@"Targets:{0},{1}
Capsule:{2},{3}", TargetA, TargetB, CurrentCapsule, CurrentCapsule.JoinedItem(this));
                    System.Diagnostics.Debug.WriteLine(output);
                }
                    /*
                            * string output = string.Format(@"Target:{0},Capsule:{1},
               Sub:{2},Join:{3},
               AlignWJ:{4}", TargetA, AlignWithJ ? J : CurrentCapsule, TargetB, AlignWithJ ? CurrentCapsule : J, AlignWithJ);
                           */
            }
            catch { }
            base.LockCapsule(lockSound);
            this.Clear();
         }

        private void Clear()
        {
            this.Target = TargetType.NoMatchAtAll;
            this.TargetA = null;
            this.TargetB = null;
            this.TargetHasVirus = false;
        }

        private void SafeDrop(vColors CA, vColors CB)
        {
            /*
            Dictionary<int, int> yMaxes = new Dictionary<int, int>();

            for (int x = 0; x < this.Width - 1; x++)
            {
                for (int y = 1; y < this.Height - 1; y++)
                {
                    if (!Locations[x, y].IsEmpty)
                    {
                        yMaxes.Add(x, y);
                        break;
                    }
                }
            }

            if (yMaxes.Any(y => y.Value == this.Height - 2))
            {
                var floor = yMaxes.Where(y=>y.Value == this.Height-2).First();
                this.TargetA = Locations[floor.Key, floor.Value];
                this.TargetB = Locations[floor.Key, floor.Value - 1];
                this.TargetHasVirus = false;
                return;
            }

            var bestSpots = yMaxes.Where(y => y.Value == yMaxes.Max(ym => ym.Value));
            if (bestSpots.Count() > 2)
            {
                // Check for joining locations, horizontal layout.
                var bestSpotsA = bestSpots.OrderBy(bs => bs.Key).ToArray();
                for(int bs = 0; bs < bestSpots.Count()-2; bs++)
                    if (bestSpotsA[bs].Key + 1 == bestSpotsA[bs + 1].Key)
                    {
                        this.TargetA = Locations[bestSpotsA[bs].Key, bestSpotsA[bs].Value];
                        this.TargetB = Locations[bestSpotsA[bs + 1].Key, bestSpotsA[bs + 1].Value];
                        this.TargetHasVirus = false;
                    }
                
            }
            if (this.TargetA == null)
            {
                this.TargetA = Locations[bestSpots.First().Key, bestSpots.First().Value];
                this.TargetB = Locations[bestSpots.First().Key, bestSpots.First().Value - 1];
                this.TargetHasVirus = false;
            }
            Output.WriteLine(string.Join("|", yMaxes.Select(y => y.Value.ToString("00"))));
             */

            Dictionary<int, TargetScan> depthTest = new Dictionary<int, TargetScan>();

            //    List<Location> validA = new List<Location>();
            //    List<Location> validB = new List<Location>();
            // first find the highest spot for CA
            for (int x = 0; x < this.Width; x++)
            {
                var dT = new TargetScan();
                for (int y = Math.Max(1, CurrentCapsule.Y); y < this.Height; y++)
                {
                    if (!Locations[x, y].IsEmpty &&
                        !Locations[x, y].Equals(CurrentCapsule) &&
                        !Locations[x, y].Equals(CurrentCapsule.JoinedItem(this)))
                    {
                        dT.Depth = y;
                        dT.Color = Locations[x, y].Color;
                        dT.Direction = TargetScan.TargetDirection.Vertical;

                        //while (y + 1 < this.Height && Locations[x, ++y].Color == dT.Color) ;
                        //dT.Length = y - dT.Depth;

                        //for (int yD = dT.Depth; yD < y; yD++)
                        //{
                        //    if (Locations[x, yD].Item.Locked)
                        //    {
                        //        dT.HasVirus = true;
                        //        break;
                        //    }
                        //}

                        //if (Locations[x, y].Color == CA)
                        //    validA.Add(Locations[x, y]);
                        //if (Locations[x, y].Color == CB)
                        //    validB.Add(Locations[x, y]);

                        break;
                    }
                }
                if (dT.Color == vColors.NULL)
                    dT.Depth = this.Height - 1;
                depthTest.Add(x, dT);
            }
            var target = depthTest
                .OrderByDescending(dt => dt.Value.Depth)
                .First();
            TargetA = Locations[target.Key, target.Value.Depth];
            TargetB = Locations[target.Key, target.Value.Depth - 1];
        }

        
       
        private struct TargetScan
        {
            public int Depth;
            public bool HasVirus { get { return VirusCount > 0; } }
            public int Length;
            public vColors Color;
            public TargetDirection Direction;
            public enum TargetDirection { Horizontal, Vertical }
            public int VirusCount;
        }

        private bool CheckNextLocation(Location start, vColors nextColor)
        {
            for (int y = start.Y + 1; y < this.Height; y++)
            {
                if (Locations[start.X, y].IsEmpty)
                    continue;
                else return (Locations[start.X, y].Color == nextColor);
            }
            return false;
        }


        private bool CheckNextLocationIsVirus(Location start, vColors nextColor)
        {
            for (int y = start.Y + 1; y < this.Height; y++)
            {
                if (Locations[start.X, y].IsEmpty)
                    continue;
                else if (Locations[start.X, y].Color == nextColor)
                {
                    bool rValue = false;
                    while (Locations[start.X, y].Color == nextColor && y < this.Height)
                    {
                        rValue = Locations[start.X, y].Item.Locked | rValue;
                        y++;
                    }
                    return rValue;
                }
                return false;
            }
            return false;
        }

        private Dictionary<Location, List<SlimDX.Vector2>> Search()
        {
            List<Location> openLocation = new List<Location>();

            for (int x = 0; x < this.Width; x++)
            {
                for (int y = 1; y < this.Height; y++)
                {
                    if (Locations[x, y].IsEmpty)
                        continue;
                    int neighbors = Locations[x, y].SearchScan(this).Count();

                    if (neighbors == 3)
                        continue;
                    else if ((x == 0 || x == this.Width - 1) && neighbors == 2)
                        continue;
                    else
                        openLocation.Add(Locations[x, y]);
                }
            }
            Pathfinder pf = new Pathfinder(this);
            var homePoint = new System.Drawing.Point(3, 1);
            var homeVector = new SlimDX.Vector2(3,1);
            Dictionary<Location, List<SlimDX.Vector2>> reachableTargets = new Dictionary<Location, List<SlimDX.Vector2>>();
            foreach (var loc in openLocation)
            {
                var positions = pf.FindPath(loc.Position, homePoint);
                if (positions.Count > 1 && positions.LastOrDefault().Equals(homeVector))
                    reachableTargets.Add(loc, positions);
            }
            return reachableTargets;

        }

        /// <summary>
        /// Sets TargetA and TargetB based on found matches.
        /// </summary>
        /// <param name="matches"></param>
        /// <param name="CA"></param>
        /// <param name="CB"></param>
        private void GetBestMatch(
            Dictionary<Location, List<SlimDX.Vector2>> matches 
            , vColors CA
            , vColors CB)
        {
            Dictionary<KeyValuePair<Location, List<SlimDX.Vector2>>, TargetScan> depthTest =new Dictionary<KeyValuePair<Location,List<SlimDX.Vector2>>,TargetScan>();
            var matchesLocal = matches.Where(m => m.Key.Color == CA || m.Key.Color == CB);
            foreach(var match in matchesLocal)
            {
                #region Target Scan
                var dT = new TargetScan();
                dT.Color = match.Key.Color;
                dT.Depth = match.Value.Count;

                dT.Direction = match.Value[1].X == match.Key.X ? TargetScan.TargetDirection.Horizontal : TargetScan.TargetDirection.Vertical;
                if (dT.Direction == TargetScan.TargetDirection.Vertical)
                {
                    for (int y = match.Key.Y; y < this.Height && Locations[match.Key.X, y].Color == match.Key.Color; y++)
                    {
                        dT.Length++;
                        if(Locations[match.Key.X, y].Item.Locked)
                           dT.VirusCount++;
                    }
                    for (int y = match.Key.Y - 2; y > 1 && Locations[match.Key.X, y].Color == match.Key.Color; y--)
                    {
                        dT.Length++;
                        if(Locations[match.Key.X, y].Item.Locked)
                            dT.VirusCount++;
                    }

                }
                else
                {
                    var np = match.Value[1];
                    if (np.Y < this.Height - 2 && !(!Locations[(int)np.X, (int)np.Y + 1].IsEmpty || (Locations[(int)np.X, (int)np.Y + 1].IsEmpty && !Locations[(int)np.X, (int)np.Y + 2].IsEmpty)))
                        continue;

                    for (int x = (int)np.X+1; x < this.Width && Locations[x, match.Key.Y].Color == match.Key.Color; x++)
                    {
                        dT.Length++;
                        if( Locations[x, match.Key.Y].Item.Locked)
                            dT.VirusCount++;
                    }
                    for (int x = (int)np.X-1; x >= 0 && Locations[x, match.Key.Y].Color == match.Key.Color; x--)
                    {
                        dT.Length++;
                        if( Locations[x, match.Key.Y].Item.Locked)
                            dT.VirusCount++;
                    }
                }
                /*
                if (x < 3}
                {
                    var xx = 3;
                    for (; xx >= x; xx--)
                    {
                        if (Locations[xx, y].IsEmpty)
                            continue;
                        else
                            break;
                    }
                    if(xx == 3) 
                        continue;
                    else if (xx >= x)
                    {
                        y++;
                        if (y < this.Height &&
                            (Locations[x-1,y].IsEmpty || false))  
                            goto DepthTest;
                        else
                            continue;
                    }
                    // if xx < x, allow to continue normally
                }
                else if (x > 4)
                {
                    var xx = 4;
                    for (; xx <= x; xx++)
                    {
                        if (Locations[xx, y].IsEmpty)
                            continue;
                        else
                            break;
                    }
                    if (xx <= x)
                    {
                        y++;
                        if (y < this.Height)
                            goto DepthTest;
                        else
                            continue;
                    }
                    // if xx > x, allow to continue normally
                }
                for (; y < this.Height; y++)
                {
                    if (!Locations[x, y].IsEmpty &&
                        !Locations[x, y].Equals(CurrentCapsule) &&
                        !Locations[x, y].Equals(CurrentCapsule.JoinedItem(this)))
                    {
                        dT.Depth = y;
                        dT.Color = Locations[x, y].Color;
                        dT.Direction = TargetScan.TargetDirection.Vertical;

                        while (y + 1 < this.Height && Locations[x, ++y].Color == dT.Color) ;
                        dT.Length = y - dT.Depth;

                        for (int yD = dT.Depth; yD < y; yD++)
                        {
                            if (Locations[x, yD].Item.Locked)
                            {
                                dT.HasVirus = true;
                                break;
                            }
                        }

                        //if (Locations[x, y].Color == CA)
                        //    validA.Add(Locations[x, y]);
                        //if (Locations[x, y].Color == CB)
                        //    validB.Add(Locations[x, y]);

                        break;
                    }
                }
                if (dT.Color == vColors.NULL)
                    dT.Depth = this.Height;
                */
                
                depthTest.Add(match, dT);
                #endregion
            }

            this.Target = TargetType.NoMatchAtAll;
            this.TargetHasVirus = false;

            #region Single Color Capsule
            if (CA == CB && depthTest.Count() > 0)
            {
                #region Viruses take priority
                if (depthTest.Any(sm => sm.Value.HasVirus))
                {
                    // Look for two virii together same color as capsule.
                    if (depthTest.Any(sm => sm.Value.VirusCount == 2 && sm.Value.Length == 2))
                    { // GO GO GO
                        // First Vertical, since its normally easier
                        if (depthTest.Any(sm => sm.Value.VirusCount == 2 && sm.Value.Length == 2 && sm.Value.Direction == TargetScan.TargetDirection.Vertical))
                        {
                            TargetA = depthTest.Where(sm => sm.Value.VirusCount == 2 && sm.Value.Length == 2 && sm.Value.Direction == TargetScan.TargetDirection.Vertical)
                            .OrderBy(sm => sm.Key.Key.Y)
                            .First()
                            .Key.Key;
                            TargetB = Locations[TargetA.X, TargetA.Y - 1];
                            this.Target = TargetType.FullMatchVertical;
                            this.TargetHasVirus = true;

                        }
                        else
                        {
                            TargetA = depthTest.Where(sm => sm.Value.VirusCount == 2 && sm.Value.Length == 2 && sm.Value.Direction == TargetScan.TargetDirection.Horizontal)
                            .OrderBy(sm => sm.Key.Key.Y)
                            .First()
                            .Key.Key;
                            TargetB = Locations[TargetA.X, TargetA.Y - 1];
                            this.Target = TargetType.FullMatchHorizontal;
                            this.TargetHasVirus = true;
                        }
                        Output.WriteLine(string.Format("Target: {0}, Virus: True", this.Target));
                        return;

                    }
                    // Find a horizontal location where target lies like so
                    /*
                    * OOOXXOOO
                    * OOOOOOOO
                    */
                    else if (depthTest.Join(depthTest, dta => new System.Drawing.Point(dta.Key.Key.Position.X + 1, dta.Key.Key.Position.Y), dtb => new System.Drawing.Point(dtb.Key.Key.Position.X, dtb.Key.Key.Position.Y), (dta, dtb) => new { A = dta, B = dtb }).Count() > 0)
                    {
                        var joinDepth = depthTest.Join(depthTest, dta => new System.Drawing.Point(dta.Key.Key.Position.X + 1, dta.Key.Key.Position.Y), dtb => new System.Drawing.Point(dtb.Key.Key.Position.X, dtb.Key.Key.Position.Y), (dta, dtb) => new { A = dta, B = dtb })
                            .OrderByDescending(a => Math.Max(a.A.Value.Depth, a.B.Value.Depth));

                        TargetA = joinDepth.First().A.Key.Key;
                        TargetB = Locations[TargetA.X + 1, TargetA.Y];
                        this.Target = TargetType.FullMatchVertical;
                        this.TargetHasVirus = TargetA.IsLocked || TargetB.IsLocked;
                        return;
                    }
                    else  // still?!?
                    {

                        this.TargetA = depthTest.Where(dt => dt.Value.HasVirus)
                       .OrderBy(dt => dt.Value.Depth)
                       .First().Key.Key;
                        this.TargetB = Locations[TargetA.X, TargetA.Y - 1];
                        this.Target = TargetType.FullMatchVertical;
                        this.TargetHasVirus = true;
                        Output.WriteLine("vv vert found");
                        return;
                    }
                }
                #endregion
                else
                {
                    this.TargetA = depthTest
                        .OrderBy(sm => sm.Value.Depth)
                        .ThenByDescending(sm => sm.Value.Length)
                        .First().Key.Key;
                    this.TargetB = Locations[TargetA.X, TargetA.Y - 1];
                    this.Target = TargetType.FullMatchVertical;
                    this.TargetHasVirus = false;
                    Output.WriteLine("cc vert found");
                    return;
                }
            }
            #endregion

            var singleMatches = depthTest.Where(dt => dt.Value.Color == CA || dt.Value.Color == CB);
            
            // Match both colors of the capsule.
            var doubleMatches = singleMatches.Where(m => m.Value.Color == CA &&
                (depthTest.Any(dt=>dt.Value.Color == CB && Math.Abs(m.Key.Key.X - dt.Key.Key.X)==1 && Math.Abs(m.Key.Key.Y-dt.Key.Key.Y) < 2)))
          //      ((depthTest.Any(dt=>dt.Key.Key.X - 1 == m.Key.Key.X && depthTest[m.Key - 1].Color == CB) || (depthTest.ContainsKey(m.Key + 1) && depthTest[m.Key + 1].Color == CB)))
                .Select(m => new { A = m, B = depthTest.Where(dt=>dt.Value.Color == CB && Math.Abs(m.Key.Key.X - dt.Key.Key.X)==1 && Math.Abs(m.Key.Key.Y-dt.Key.Key.Y) < 2).OrderByDescending(dt=>dt.Value.Length).First()});

                    
                  //  (depthTest.ContainsKey(m.Key - 1) && depthTest[m.Key - 1].Color == CB) ? new KeyValuePair<int, TargetScan>(m.Key - 1, depthTest[m.Key - 1]) : new KeyValuePair<int, TargetScan>(m.Key + 1, depthTest[m.Key + 1]) });
            
            var singleMatchesRefined = singleMatches.Where(m => (
                (depthTest.Keys.Any(k => k.Key.X == m.Key.Key.X - 1) && depthTest[depthTest.Keys.Where(k => k.Key.X == m.Key.Key.X - 1).First()].Depth >= m.Value.Depth + 4) ||
                (depthTest.Keys.Any(k => k.Key.X == m.Key.Key.X + 1) && depthTest[depthTest.Keys.Where(k => k.Key.X == m.Key.Key.X + 1).First()].Depth >= m.Value.Depth + 4)));

            if (doubleMatches.Any(dm => Math.Min(dm.A.Value.Depth, dm.B.Value.Depth) < 5 &&
                (dm.A.Value.Depth < dm.B.Value.Depth ? (dm.A.Value.Length + 1 == 4 || (dm.A.Value.Depth - 1 > 1)) : (dm.B.Value.Length + 1 == 4 || (dm.B.Value.Depth - 1 > 1)))))
            {
                var target = doubleMatches.Where(dm => Math.Min(dm.A.Value.Depth, dm.B.Value.Depth) < 5 &&
                (dm.A.Value.Depth < dm.B.Value.Depth ? (dm.A.Value.Length + 1 == 4 || (dm.A.Value.Depth - 1 > 1)) : (dm.B.Value.Length + 1 == 4 || (dm.B.Value.Depth - 1 > 1))))
                    .OrderBy(dm => Math.Min(dm.A.Value.Depth, dm.B.Value.Depth))
                    .FirstOrDefault();
                TargetA = target.A.Key.Key;// Locations[target.A.Key, target.A.Value.Depth];
                TargetB = target.B.Key.Key;// Locations[target.B.Key, target.B.Value.Depth];
                this.Target = TargetType.FullMatchVertical;
                this.TargetHasVirus = target.A.Value.HasVirus || target.B.Value.HasVirus;
                Output.WriteLine("clear high double match found");
                return;
            
            }
            else if (singleMatches.Any(sm => sm.Value.Depth < 5 &&
                (sm.Value.Length + 1 == 4 || (sm.Value.Depth - 1 > 1))))
            {
                if (singleMatches.Any(sm => sm.Value.Depth < 5 && sm.Value.Length == 3 && CheckNextLocation(Locations[sm.Key.Key.X, sm.Value.Depth + sm.Value.Length], sm.Value.Color == CA ? CB : CA)))
                {
                    var target = singleMatches.Where(sm => sm.Value.Depth < 5 && 
                        sm.Value.Length == 3 && 
                        CheckNextLocation(Locations[sm.Key.Key.X, sm.Value.Depth + sm.Value.Length], sm.Value.Color == CA ? CB : CA))
                         .OrderBy(sm => sm.Value.Depth)
                         .First();
                    TargetA = Locations[target.Key.Key.X, target.Value.Depth];
                    TargetB = Locations[target.Key.Key.X, target.Value.Depth - 1];
                    this.TargetHasVirus = target.Value.HasVirus;
                    this.Target = TargetType.HalfMatchVertical;
                    Output.WriteLine("clear high single drop through");
                    Output.WriteLine(string.Format("Color: {0}, X: {1}, Depth: {2}, Length: {3}", target.Value.Color.ToString()[0], target.Key, target.Value.Depth, target.Value.Length));
                }
                else
                {
                    var target = singleMatches.Where(sm => sm.Value.Depth < 5 &&
                        (sm.Value.Length + 1 == 4 || (sm.Value.Depth - 1 > 1)))
                         .OrderBy(sm => sm.Value.Depth)
                         .First();
                    TargetA = Locations[target.Key.Key.X, target.Value.Depth];
                    TargetB = null;
                    this.TargetHasVirus = target.Value.HasVirus;
                    this.Target = TargetType.HalfMatchVertical;
                    Output.WriteLine("clear high single capsule");
                    Output.WriteLine(string.Format("Color: {0}, X: {1}, Depth: {2}, Length: {3}", target.Value.Color.ToString()[0], target.Key, target.Value.Depth, target.Value.Length));
            
                }
            }

            else if (doubleMatches.Any(dm => dm.A.Value.HasVirus && dm.B.Value.HasVirus))
            {
                var target = doubleMatches.Where(dm => dm.A.Value.HasVirus && dm.B.Value.HasVirus)
                    .OrderBy(dm => Math.Min(dm.A.Value.Depth, dm.B.Value.Depth))
                    .FirstOrDefault();
                TargetA = Locations[target.A.Key.Key.X, target.A.Value.Depth];
                TargetB = Locations[target.B.Key.Key.X, target.B.Value.Depth];
                this.Target = TargetType.FullMatchVertical;
                this.TargetHasVirus = true;
                Output.WriteLine("vv match found");
                return;
            }
            else if (singleMatches.Any(sm => sm.Value.Length == 3 && sm.Value.HasVirus && CheckNextLocationIsVirus(Locations[sm.Key.Key.X, sm.Value.Depth + sm.Value.Length], sm.Value.Color == CA ? CB : CA)))
            {
                var smv = singleMatches.Where(sm => sm.Value.HasVirus && CheckNextLocationIsVirus(Locations[sm.Key.Key.X, sm.Value.Depth + sm.Value.Length], sm.Value.Color == CA ? CB : CA));
                var targetsmv = smv
                .Where(sm => sm.Value.HasVirus)
                .OrderBy(sm => sm.Value.Depth)
                .First();
                TargetA = Locations[targetsmv.Key.Key.X, targetsmv.Value.Depth];
                TargetB = Locations[targetsmv.Key.Key.X, targetsmv.Value.Depth - 1];
                this.Target = TargetType.FullMatchVertical;
                this.TargetHasVirus = true;
                Output.WriteLine("vv + below match found");
                return;

            }
            else if (doubleMatches.Any(dm => dm.A.Value.HasVirus || dm.B.Value.HasVirus))
            {
                var target = doubleMatches.Where(dm => dm.A.Value.HasVirus || dm.B.Value.HasVirus)
                    .OrderBy(dm => Math.Min(dm.A.Value.Depth, dm.B.Value.Depth))
                    .FirstOrDefault();
                TargetA = Locations[target.A.Key.Key.X, target.A.Value.Depth];
                TargetB = Locations[target.B.Key.Key.X, target.B.Value.Depth];
                this.Target = TargetType.FullMatchVertical;
                this.TargetHasVirus = true;
                Output.WriteLine("vc match found");
                return;

            }
            else if (singleMatches.Any(sm => sm.Value.Length == 3 && (sm.Value.HasVirus || CheckNextLocationIsVirus(Locations[sm.Key.Key.X, sm.Value.Depth + sm.Value.Length], sm.Value.Color == CA ? CB : CA))))
            {
                var targetsmv = singleMatches.Where(sm => sm.Value.Length == 3 && (sm.Value.HasVirus || CheckNextLocationIsVirus(Locations[sm.Key.Key.X, sm.Value.Depth + sm.Value.Length], sm.Value.Color == CA ? CB : CA)))
                    .OrderByDescending(sm => sm.Value.Depth)
                    .First();
                TargetA = Locations[targetsmv.Key.Key.X, targetsmv.Value.Depth];
                TargetB = Locations[targetsmv.Key.Key.X, targetsmv.Value.Depth - 1];
                this.Target = TargetType.HalfMatchVertical;
                this.TargetHasVirus = true;
                Output.WriteLine("sv + below match found");
                return;

            }

            else if (singleMatchesRefined.Any(sm => sm.Value.HasVirus))
            {
                // First priority goes to any with a valid color underneath.
                var target = singleMatchesRefined
                    .Where(sm => sm.Value.HasVirus)
                    .OrderByDescending(sm => sm.Value.Length)
                    .ThenByDescending(sm => sm.Value.Depth)
                    .First();
                TargetA = Locations[target.Key.Key.X, target.Value.Depth];
                TargetB = null;
                this.Target = TargetType.HalfMatchVertical;
                this.TargetHasVirus = true;
                Output.WriteLine("sv match found");
                return;

            }
            else if (doubleMatches.Count() > 0)
            {
                var target = doubleMatches
                       .OrderBy(dm => Math.Min(dm.A.Value.Depth, dm.B.Value.Depth))
                       .FirstOrDefault();
                TargetA = Locations[target.A.Key.Key.X, target.A.Value.Depth];
                TargetB = Locations[target.B.Key.Key.X, target.B.Value.Depth];
                this.Target = TargetType.FullMatchVertical;
                this.TargetHasVirus = false;
                Output.WriteLine("cc match found");
                return;
            }
            else if (singleMatches.Any(sm => sm.Value.Length == 3 && CheckNextLocation(Locations[sm.Key.Key.X, sm.Value.Depth + sm.Value.Length], sm.Value.Color == CA ? CB : CA)))
            {
                var targetsmv = singleMatches.Where(sm => sm.Value.Length == 3 && CheckNextLocation(Locations[sm.Key.Key.X, sm.Value.Depth + sm.Value.Length], sm.Value.Color == CA ? CB : CA))
                    .OrderByDescending(sm => sm.Value.Depth)
                    .First();
                TargetA = Locations[targetsmv.Key.Key.X, targetsmv.Value.Depth];
                TargetB = Locations[targetsmv.Key.Key.X, targetsmv.Value.Depth - 1];
                this.Target = TargetType.HalfMatchVertical;
                this.TargetHasVirus = false;
                Output.WriteLine("sv + below match found");
                return;

            }

            else if (singleMatches.Count() > 0)
            {
                var target = singleMatches
                        .OrderByDescending(sm => sm.Value.Length)
                        .ThenByDescending(sm => sm.Value.Depth)
                        .First();
                TargetA = Locations[target.Key.Key.X, target.Value.Depth];
                TargetB = null;
                this.Target = TargetType.HalfMatchVertical;
                this.TargetHasVirus = true;
                Output.WriteLine("sc match found");
                return;
            }
        }

        private void SearchByY(vColors CA, vColors CB)
        {
            
        }
    }
}
