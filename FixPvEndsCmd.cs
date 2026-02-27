using System;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;

[assembly: CommandClass(typeof(FIXPVENDS.FixPvEndsCmd))]

namespace FIXPVENDS
{
    public class FixPvEndsCmd
    {
        // Session-level persistence for parameters
        private static double _lastOffset = 7.5;
        private static double _lastTextHeight = 2.5;
        private static string _lastValueType = "Station";

        [CommandMethod("FIXPVENDS")]
        public void FixProfileViewEnds()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    // 1. Selection
                    PromptEntityOptions peo = new PromptEntityOptions("\nSelect a Profile View: ");
                    peo.SetRejectMessage("\nSelected object is not a Profile View.");
                    peo.AddAllowedClass(typeof(ProfileView), true);

                    PromptEntityResult per = ed.GetEntity(peo);
                    if (per.Status != PromptStatus.OK) return;

                    ProfileView pv = tr.GetObject(per.ObjectId, OpenMode.ForRead) as ProfileView;
                    if (pv == null) return;

                    // 2. Alignment
                    Alignment alignment = tr.GetObject(pv.AlignmentId, OpenMode.ForRead) as Alignment;
                    if (alignment == null)
                    {
                        ed.WriteMessage("\nError: No Alignment associated with this Profile View.");
                        return;
                    }

                    // 3. Mode Selection (Station vs Elevation)
                    PromptKeywordOptions pko = new PromptKeywordOptions("\nSelect value type to insert [Station/Elevation]: ");
                    pko.Keywords.Add("Station");
                    pko.Keywords.Add("Elevation");
                    pko.Keywords.Default = _lastValueType;
                    pko.AllowNone = true;

                    PromptResult pr = ed.GetKeywords(pko);
                    if (pr.Status == PromptStatus.OK)
                    {
                        _lastValueType = pr.StringResult;
                    }

                    // 4. User Parameters for placement (Persistent)
                    PromptDoubleOptions pdoOffset = new PromptDoubleOptions("\nEnter vertical offset from grid bottom: ");
                    pdoOffset.DefaultValue = _lastOffset;
                    pdoOffset.UseDefaultValue = true;
                    PromptDoubleResult pdrOffset = ed.GetDouble(pdoOffset);
                    if (pdrOffset.Status == PromptStatus.OK)
                    {
                        _lastOffset = pdrOffset.Value;
                    }

                    PromptDoubleOptions pdoHeight = new PromptDoubleOptions("\nEnter text height: ");
                    pdoHeight.DefaultValue = _lastTextHeight;
                    pdoHeight.UseDefaultValue = true;
                    PromptDoubleResult pdrHeight = ed.GetDouble(pdoHeight);
                    if (pdrHeight.Status == PromptStatus.OK)
                    {
                        _lastTextHeight = pdrHeight.Value;
                    }

                    // 5. Calculate Values to Insert
                    double startStation = alignment.StartingStation;
                    double endStation = alignment.EndingStation;
                    string startText = "";
                    string endText = "";

                    if (_lastValueType == "Station")
                    {
                        startText = startStation.ToString("0.000");
                        endText = endStation.ToString("0.000");
                    }
                    else // Elevation
                    {
                        Profile groundProfile = null;
                        foreach (ObjectId profileId in alignment.GetProfileIds())
                        {
                            Profile p = tr.GetObject(profileId, OpenMode.ForRead) as Profile;
                            if (p != null) { groundProfile = p; break; }
                        }

                        if (groundProfile == null)
                        {
                            ed.WriteMessage("\nError: No Profile found for this Alignment.");
                            return;
                        }

                        startText = groundProfile.ElevationAt(startStation).ToString("0.000");
                        endText = groundProfile.ElevationAt(endStation).ToString("0.000");
                    }

                    // 6. Calculate Coordinates (X, Y)
                    double targetElevation = pv.ElevationMin - _lastOffset;
                    double xStart = 0, yStart = 0;
                    double xEnd = 0, yEnd = 0;

                    pv.FindXYAtStationAndElevation(startStation, targetElevation, ref xStart, ref yStart);
                    pv.FindXYAtStationAndElevation(endStation, targetElevation, ref xEnd, ref yEnd);

                    // 7. Create MText Objects
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                    // --- START ---
                    using (MText mtStart = new MText())
                    {
                        mtStart.Contents = startText;
                        mtStart.Location = new Point3d(xStart, yStart, 0);
                        mtStart.TextHeight = _lastTextHeight;
                        mtStart.Rotation = Math.PI / 2.0; // 90 Degrees
                        mtStart.Attachment = AttachmentPoint.MiddleCenter;
                        
                        btr.AppendEntity(mtStart);
                        tr.AddNewlyCreatedDBObject(mtStart, true);
                    }

                    // --- END ---
                    using (MText mtEnd = new MText())
                    {
                        mtEnd.Contents = endText;
                        mtEnd.Location = new Point3d(xEnd, yEnd, 0);
                        mtEnd.TextHeight = _lastTextHeight;
                        mtEnd.Rotation = Math.PI / 2.0; // 90 Degrees
                        mtEnd.Attachment = AttachmentPoint.MiddleCenter;

                        btr.AppendEntity(mtEnd);
                        tr.AddNewlyCreatedDBObject(mtEnd, true);
                    }

                    ed.WriteMessage($"\nSuccessfully inserted {_lastValueType} values {startText} and {endText} into the band.");
                    tr.Commit();
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage("\nError: " + ex.Message);
                }
            }
        }
    }
}
