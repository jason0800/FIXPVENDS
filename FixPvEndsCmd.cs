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

                    // 3. User Parameters for placement
                    PromptDoubleOptions pdoOffset = new PromptDoubleOptions("\nEnter vertical offset from grid bottom (e.g. 7.5): ");
                    pdoOffset.DefaultValue = 7.5;
                    pdoOffset.UseDefaultValue = true;
                    PromptDoubleResult pdrOffset = ed.GetDouble(pdoOffset);
                    double vOffset = (pdrOffset.Status == PromptStatus.OK) ? pdrOffset.Value : 7.5;

                    PromptDoubleOptions pdoHeight = new PromptDoubleOptions("\nEnter text height (e.g. 2.5): ");
                    pdoHeight.DefaultValue = 2.5;
                    pdoHeight.UseDefaultValue = true;
                    PromptDoubleResult pdrHeight = ed.GetDouble(pdoHeight);
                    double textHeight = (pdrHeight.Status == PromptStatus.OK) ? pdrHeight.Value : 2.5;

                    // 4. Calculate Coordinates (X, Y)
                    // We find the exact X,Y in drawing space for the start/end stations
                    double startStation = alignment.StartingStation;
                    double endStation = alignment.EndingStation;
                    double targetElevation = pv.ElevationMin - vOffset;

                    double xStart = 0, yStart = 0;
                    double xEnd = 0, yEnd = 0;

                    pv.FindXYAtStationAndElevation(startStation, targetElevation, ref xStart, ref yStart);
                    pv.FindXYAtStationAndElevation(endStation, targetElevation, ref xEnd, ref yEnd);

                    // 5. Create MText Objects (The "Literal" Insertion)
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);

                    // --- START STATION ---
                    using (MText mtStart = new MText())
                    {
                        mtStart.Contents = startStation.ToString("0.000");
                        mtStart.Location = new Point3d(xStart, yStart, 0);
                        mtStart.TextHeight = textHeight;
                        mtStart.Rotation = Math.PI / 2.0; // 90 Degrees
                        mtStart.Attachment = AttachmentPoint.MiddleCenter;
                        
                        btr.AppendEntity(mtStart);
                        tr.AddNewlyCreatedDBObject(mtStart, true);
                    }

                    // --- END STATION ---
                    using (MText mtEnd = new MText())
                    {
                        mtEnd.Contents = endStation.ToString("0.000");
                        mtEnd.Location = new Point3d(xEnd, yEnd, 0);
                        mtEnd.TextHeight = textHeight;
                        mtEnd.Rotation = Math.PI / 2.0; // 90 Degrees
                        mtEnd.Attachment = AttachmentPoint.MiddleCenter;

                        btr.AppendEntity(mtEnd);
                        tr.AddNewlyCreatedDBObject(mtEnd, true);
                    }

                    ed.WriteMessage($"\nSuccessfully inserted MText values {startStation:F3} and {endStation:F3} into the band.");
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
