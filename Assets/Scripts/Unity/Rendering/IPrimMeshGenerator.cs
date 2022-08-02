// using System;
// using System.Collections.Generic;
// using System.IO;
// using LibreMetaverse.PrimMesher;
// using OpenMetaverse;
// using OpenMetaverse.Rendering;
// using Path = System.IO.Path;
//
// namespace UnityTemplateProjects.Unity.Rendering
// {
//     public class IPrimMeshGenerator
//     {
//         
//     }
//
//     public class PrimMeshArgs
//     {
//         private const float twoPi = 2.0f * (float) Math.PI;
//         public bool calcVertexNormals;
//
//         public List<Coord> coords;
//         public float dimpleBegin;
//         public float dimpleEnd = 1.0f;
//         public string errorMessage = "";
//         public List<Face> faces;
//
//         public float holeSizeX = 1.0f; // called pathScaleX in pbs
//         public float holeSizeY = 0.25f;
//         private readonly float hollow;
//         private readonly int hollowSides = 4;
//         public List<Coord> normals;
//         private bool normalsProcessed;
//
//         public int numPrimFaces;
//         public float pathCutBegin;
//         public float pathCutEnd = 1.0f;
//         private readonly float profileEnd = 1.0f;
//
//         private readonly float profileStart;
//         public float radius;
//         public float revolutions = 1.0f;
//
//         private readonly int sides = 4;
//         public float skew;
//         public bool sphereMode = false;
//         public int stepsPerRevolution = 24;
//         public float taperX;
//         public float taperY;
//         public float topShearX;
//         public float topShearY;
//         public int twistBegin;
//         public int twistEnd;
//
//         public List<ViewerFace> viewerFaces;
//         public bool viewerMode;
//
//
//         /// <summary>
//         ///     Constructs a PrimMesh object and creates the profile for extrusion.
//         /// </summary>
//         /// <param name="sides"></param>
//         /// <param name="profileStart"></param>
//         /// <param name="profileEnd"></param>
//         /// <param name="hollow"></param>
//         /// <param name="hollowSides"></param>
//         public PrimMeshArgs(int sides, float profileStart, float profileEnd, float hollow, int hollowSides)
//         {
//             this.sides = sides;
//             this.profileStart = profileStart;
//             this.profileEnd = profileEnd;
//             this.hollow = hollow;
//             this.hollowSides = hollowSides;
//
//             if (sides < 3)
//                 throw new ArgumentOutOfRangeException(nameof(sides),sides,"Cannot be less than 3!");
//             if (hollowSides < 3)
//                 throw new ArgumentOutOfRangeException(nameof(hollowSides),hollowSides,"Cannot be less than 3!");
//             if (profileStart < 0.0f)
//                 throw new ArgumentOutOfRangeException(nameof(profileStart),profileStart,"Cannot be less than 0.0!");
//             if (profileEnd > 1.0f)
//                 throw new ArgumentOutOfRangeException(nameof(profileEnd),profileEnd,"Cannot be greater than 1.0!");
//             if (profileEnd < 0.02f)
//                 throw new ArgumentOutOfRangeException(nameof(profileEnd),profileEnd,"Cannot be less than 0.02!");
//             if (profileStart >= profileEnd)
//                 throw new ArgumentOutOfRangeException(nameof(profileStart),profileStart,$"Cannot be greater or equal to {nameof(profileEnd)} ~ `{profileEnd}`!");
//                 // this.profileStart = profileEnd - 0.02f;
//             if (hollow > 0.99f)
//                 throw new ArgumentOutOfRangeException(nameof(hollow),hollow,"Cannot be greater than 0.99!");
//             if (hollow < 0.0f)
//                 throw new ArgumentOutOfRangeException(nameof(hollow),hollow,"Cannot be less than 0.0!");
//
//         }
//
//         public int ProfileOuterFaceNumber { get; private set; } = -1;
//
//         public int ProfileHollowFaceNumber { get; private set; } = -1;
//
//         public bool HasProfileCut { get; private set; }
//
//         public bool HasHollow { get; private set; }
//
//         /// <summary>
//         ///     Human readable string representation of the parameters used to create a mesh.
//         /// </summary>
//         /// <returns></returns>
//         public string ParamsToDisplayString()
//         {
//             var s = "";
//             s += "sides..................: " + sides;
//             s += "\nhollowSides..........: " + hollowSides;
//             s += "\nprofileStart.........: " + profileStart;
//             s += "\nprofileEnd...........: " + profileEnd;
//             s += "\nhollow...............: " + hollow;
//             s += "\ntwistBegin...........: " + twistBegin;
//             s += "\ntwistEnd.............: " + twistEnd;
//             s += "\ntopShearX............: " + topShearX;
//             s += "\ntopShearY............: " + topShearY;
//             s += "\npathCutBegin.........: " + pathCutBegin;
//             s += "\npathCutEnd...........: " + pathCutEnd;
//             s += "\ndimpleBegin..........: " + dimpleBegin;
//             s += "\ndimpleEnd............: " + dimpleEnd;
//             s += "\nskew.................: " + skew;
//             s += "\nholeSizeX............: " + holeSizeX;
//             s += "\nholeSizeY............: " + holeSizeY;
//             s += "\ntaperX...............: " + taperX;
//             s += "\ntaperY...............: " + taperY;
//             s += "\nradius...............: " + radius;
//             s += "\nrevolutions..........: " + revolutions;
//             s += "\nstepsPerRevolution...: " + stepsPerRevolution;
//             s += "\nsphereMode...........: " + sphereMode;
//             s += "\nhasProfileCut........: " + HasProfileCut;
//             s += "\nhasHollow............: " + HasHollow;
//             s += "\nviewerMode...........: " + viewerMode;
//
//             return s;
//         }
//
//         /// <summary>
//         ///     Extrudes a profile along a path.
//         /// </summary>
//         public void Extrude(PathType pathType)
//         {
//             var needEndFaces = false;
//
//             coords = new List<Coord>();
//             this.faces = new List<Face>();
//
//             if (viewerMode)
//             {
//                 viewerFaces = new List<ViewerFace>();
//                 calcVertexNormals = true;
//             }
//
//             if (calcVertexNormals)
//                 normals = new List<Coord>();
//
//             var steps = 1;
//
//             var length = pathCutEnd - pathCutBegin;
//             normalsProcessed = false;
//
//             if (viewerMode && sides == 3)
//                 if (Math.Abs(taperX) > 0.01 || Math.Abs(taperY) > 0.01)
//                     steps = (int) (steps * 4.5 * length);
//
//             if (sphereMode)
//                 HasProfileCut = profileEnd - profileStart < 0.4999f;
//             else
//                 HasProfileCut = profileEnd - profileStart < 0.9999f;
//             HasHollow = this.hollow > 0.001f;
//
//             var twistBegin = this.twistBegin / 360.0f * twoPi;
//             var twistEnd = this.twistEnd / 360.0f * twoPi;
//             var twistTotal = twistEnd - twistBegin;
//             var twistTotalAbs = Math.Abs(twistTotal);
//             if (twistTotalAbs > 0.01f)
//                 steps += (int) (twistTotalAbs * 3.66); //  dahlia's magic number
//
//             var hollow = this.hollow;
//
//             if (pathType == PathType.Circular)
//             {
//                 needEndFaces = false;
//                 if (pathCutBegin != 0.0f || pathCutEnd != 1.0f)
//                     needEndFaces = true;
//                 else if (taperX != 0.0f || taperY != 0.0f)
//                     needEndFaces = true;
//                 else if (skew != 0.0f)
//                     needEndFaces = true;
//                 else if (twistTotal != 0.0f)
//                     needEndFaces = true;
//                 else if (radius != 0.0f)
//                     needEndFaces = true;
//             }
//             else
//             {
//                 needEndFaces = true;
//             }
//
//             // sanity checks
//             var initialProfileRot = 0.0f;
//             if (pathType == PathType.Circular)
//             {
//                 switch (sides)
//                 {
//                     case 3:
//                         initialProfileRot = (float) Math.PI;
//                         if (hollowSides == 4)
//                         {
//                             if (hollow > 0.7f)
//                                 hollow = 0.7f;
//                             hollow *= 0.707f;
//                         }
//                         else
//                         {
//                             hollow *= 0.5f;
//                         }
//                         break;
//                     case 4:
//                         initialProfileRot = 0.25f * (float) Math.PI;
//                         if (hollowSides != 4)
//                             hollow *= 0.707f;
//                         break;
//                     default:
//                         if (sides > 4)
//                         {
//                             initialProfileRot = (float) Math.PI;
//                             if (hollowSides == 4)
//                             {
//                                 if (hollow > 0.7f)
//                                     hollow = 0.7f;
//                                 hollow /= 0.7f;
//                             }
//                         }
//                         break;
//                 }
//             }
//             else
//             {
//                 switch (sides)
//                 {
//                     case 3:
//                         if (hollowSides == 4)
//                         {
//                             if (hollow > 0.7f)
//                                 hollow = 0.7f;
//                             hollow *= 0.707f;
//                         }
//                         else
//                         {
//                             hollow *= 0.5f;
//                         }
//                         break;
//                     case 4:
//                         initialProfileRot = 1.25f * (float) Math.PI;
//                         if (hollowSides != 4)
//                             hollow *= 0.707f;
//                         break;
//                     case 24 when hollowSides == 4:
//                         hollow *= 1.414f;
//                         break;
//                 }
//             }
//
//             var profile = new Profile(sides, profileStart, profileEnd, hollow, hollowSides, true, calcVertexNormals);
//             errorMessage = profile.errorMessage;
//
//             numPrimFaces = profile.numPrimFaces;
//
//             var cut1FaceNumber = profile.bottomFaceNumber + 1;
//             var cut2FaceNumber = cut1FaceNumber + 1;
//             if (!needEndFaces)
//             {
//                 cut1FaceNumber -= 2;
//                 cut2FaceNumber -= 2;
//             }
//
//             ProfileOuterFaceNumber = profile.outerFaceNumber;
//             if (!needEndFaces)
//                 ProfileOuterFaceNumber--;
//
//             if (HasHollow)
//             {
//                 ProfileHollowFaceNumber = profile.hollowFaceNumber;
//                 if (!needEndFaces)
//                     ProfileHollowFaceNumber--;
//             }
//
//             var cut1Vert = -1;
//             var cut2Vert = -1;
//             if (HasProfileCut)
//             {
//                 cut1Vert = HasHollow ? profile.coords.Count - 1 : 0;
//                 cut2Vert = HasHollow ? profile.numOuterVerts - 1 : profile.numOuterVerts;
//             }
//
//             if (initialProfileRot != 0.0f)
//             {
//                 profile.AddRot(new Quat(new Coord(0.0f, 0.0f, 1.0f), initialProfileRot));
//                 if (viewerMode)
//                     profile.MakeFaceUVs();
//             }
//
//             var lastCutNormal1 = new Coord();
//             var lastCutNormal2 = new Coord();
//             var thisV = 0.0f;
//             var lastV = 0.0f;
//
//             var path = new Path
//             {
//                 twistBegin = twistBegin,
//                 twistEnd = twistEnd,
//                 topShearX = topShearX,
//                 topShearY = topShearY,
//                 pathCutBegin = pathCutBegin,
//                 pathCutEnd = pathCutEnd,
//                 dimpleBegin = dimpleBegin,
//                 dimpleEnd = dimpleEnd,
//                 skew = skew,
//                 holeSizeX = holeSizeX,
//                 holeSizeY = holeSizeY,
//                 taperX = taperX,
//                 taperY = taperY,
//                 radius = radius,
//                 revolutions = revolutions,
//                 stepsPerRevolution = stepsPerRevolution
//             };
//
//             path.Create(pathType, steps);
//
//             for (var nodeIndex = 0; nodeIndex < path.pathNodes.Count; nodeIndex++)
//             {
//                 var node = path.pathNodes[nodeIndex];
//                 var newLayer = profile.Copy();
//                 newLayer.Scale(node.xScale, node.yScale);
//
//                 newLayer.AddRot(node.rotation);
//                 newLayer.AddPos(node.position);
//
//                 if (needEndFaces && nodeIndex == 0)
//                 {
//                     newLayer.FlipNormals();
//
//                     // add the bottom faces to the viewerFaces list
//                     if (viewerMode)
//                     {
//                         var faceNormal = newLayer.faceNormal;
//                         var newViewerFace = new ViewerFace(profile.bottomFaceNumber);
//                         var numFaces = newLayer.faces.Count;
//                         var faces = newLayer.faces;
//
//                         for (var i = 0; i < numFaces; i++)
//                         {
//                             var face = faces[i];
//                             newViewerFace.v1 = newLayer.coords[face.v1];
//                             newViewerFace.v2 = newLayer.coords[face.v2];
//                             newViewerFace.v3 = newLayer.coords[face.v3];
//
//                             newViewerFace.coordIndex1 = face.v1;
//                             newViewerFace.coordIndex2 = face.v2;
//                             newViewerFace.coordIndex3 = face.v3;
//
//                             newViewerFace.n1 = faceNormal;
//                             newViewerFace.n2 = faceNormal;
//                             newViewerFace.n3 = faceNormal;
//
//                             newViewerFace.uv1 = newLayer.faceUVs[face.v1];
//                             newViewerFace.uv2 = newLayer.faceUVs[face.v2];
//                             newViewerFace.uv3 = newLayer.faceUVs[face.v3];
//
//                             if (pathType == PathType.Linear)
//                             {
//                                 newViewerFace.uv1.Flip();
//                                 newViewerFace.uv2.Flip();
//                                 newViewerFace.uv3.Flip();
//                             }
//
//                             viewerFaces.Add(newViewerFace);
//                         }
//                     }
//                 } // if (nodeIndex == 0)
//
//                 // append this layer
//
//                 var coordsLen = coords.Count;
//                 newLayer.AddValue2FaceVertexIndices(coordsLen);
//
//                 coords.AddRange(newLayer.coords);
//
//                 if (calcVertexNormals)
//                 {
//                     newLayer.AddValue2FaceNormalIndices(normals.Count);
//                     normals.AddRange(newLayer.vertexNormals);
//                 }
//
//                 if (node.percentOfPath < pathCutBegin + 0.01f || node.percentOfPath > pathCutEnd - 0.01f)
//                     this.faces.AddRange(newLayer.faces);
//
//                 // fill faces between layers
//
//                 var numVerts = newLayer.coords.Count;
//                 var newFace1 = new Face();
//                 var newFace2 = new Face();
//
//                 thisV = 1.0f - node.percentOfPath;
//
//                 if (nodeIndex > 0)
//                 {
//                     var startVert = coordsLen + 1;
//                     var endVert = coords.Count;
//
//                     if (sides < 5 || HasProfileCut || HasHollow)
//                         startVert--;
//
//                     for (var i = startVert; i < endVert; i++)
//                     {
//                         var iNext = i + 1;
//                         if (i == endVert - 1)
//                             iNext = startVert;
//
//                         var whichVert = i - startVert;
//
//                         newFace1.v1 = i;
//                         newFace1.v2 = i - numVerts;
//                         newFace1.v3 = iNext;
//
//                         newFace1.n1 = newFace1.v1;
//                         newFace1.n2 = newFace1.v2;
//                         newFace1.n3 = newFace1.v3;
//                         faces.Add(newFace1);
//
//                         newFace2.v1 = iNext;
//                         newFace2.v2 = i - numVerts;
//                         newFace2.v3 = iNext - numVerts;
//
//                         newFace2.n1 = newFace2.v1;
//                         newFace2.n2 = newFace2.v2;
//                         newFace2.n3 = newFace2.v3;
//                         faces.Add(newFace2);
//
//                         if (viewerMode)
//                         {
//                             // add the side faces to the list of viewerFaces here
//
//                             var primFaceNum = profile.faceNumbers[whichVert];
//                             if (!needEndFaces)
//                                 primFaceNum -= 1;
//
//                             var newViewerFace1 = new ViewerFace(primFaceNum);
//                             var newViewerFace2 = new ViewerFace(primFaceNum);
//
//                             var uIndex = whichVert;
//                             if (!HasHollow && sides > 4 && uIndex < newLayer.us.Count - 1)
//                                 uIndex++;
//
//                             var u1 = newLayer.us[uIndex];
//                             var u2 = 1.0f;
//                             if (uIndex < newLayer.us.Count - 1)
//                                 u2 = newLayer.us[uIndex + 1];
//
//                             if (whichVert == cut1Vert || whichVert == cut2Vert)
//                             {
//                                 u1 = 0.0f;
//                                 u2 = 1.0f;
//                             }
//                             else if (sides < 5)
//                             {
//                                 if (whichVert < profile.numOuterVerts)
//                                 {
//                                     // boxes and prisms have one texture face per side of the prim, so the U values have to be scaled
//                                     // to reflect the entire texture width
//                                     u1 *= sides;
//                                     u2 *= sides;
//                                     u2 -= (int) u1;
//                                     u1 -= (int) u1;
//                                     if (u2 < 0.1f)
//                                         u2 = 1.0f;
//                                 }
//                             }
//
//                             if (sphereMode)
//                                 if (whichVert != cut1Vert && whichVert != cut2Vert)
//                                 {
//                                     u1 = u1 * 2.0f - 1.0f;
//                                     u2 = u2 * 2.0f - 1.0f;
//
//                                     if (whichVert >= newLayer.numOuterVerts)
//                                     {
//                                         u1 -= hollow;
//                                         u2 -= hollow;
//                                     }
//                                 }
//
//                             newViewerFace1.uv1.U = u1;
//                             newViewerFace1.uv2.U = u1;
//                             newViewerFace1.uv3.U = u2;
//
//                             newViewerFace1.uv1.V = thisV;
//                             newViewerFace1.uv2.V = lastV;
//                             newViewerFace1.uv3.V = thisV;
//
//                             newViewerFace2.uv1.U = u2;
//                             newViewerFace2.uv2.U = u1;
//                             newViewerFace2.uv3.U = u2;
//
//                             newViewerFace2.uv1.V = thisV;
//                             newViewerFace2.uv2.V = lastV;
//                             newViewerFace2.uv3.V = lastV;
//
//                             newViewerFace1.v1 = coords[newFace1.v1];
//                             newViewerFace1.v2 = coords[newFace1.v2];
//                             newViewerFace1.v3 = coords[newFace1.v3];
//
//                             newViewerFace2.v1 = coords[newFace2.v1];
//                             newViewerFace2.v2 = coords[newFace2.v2];
//                             newViewerFace2.v3 = coords[newFace2.v3];
//
//                             newViewerFace1.coordIndex1 = newFace1.v1;
//                             newViewerFace1.coordIndex2 = newFace1.v2;
//                             newViewerFace1.coordIndex3 = newFace1.v3;
//
//                             newViewerFace2.coordIndex1 = newFace2.v1;
//                             newViewerFace2.coordIndex2 = newFace2.v2;
//                             newViewerFace2.coordIndex3 = newFace2.v3;
//
//                             // profile cut faces
//                             if (whichVert == cut1Vert)
//                             {
//                                 newViewerFace1.primFaceNumber = cut1FaceNumber;
//                                 newViewerFace2.primFaceNumber = cut1FaceNumber;
//                                 newViewerFace1.n1 = newLayer.cutNormal1;
//                                 newViewerFace1.n2 = newViewerFace1.n3 = lastCutNormal1;
//
//                                 newViewerFace2.n1 = newViewerFace2.n3 = newLayer.cutNormal1;
//                                 newViewerFace2.n2 = lastCutNormal1;
//                             }
//                             else if (whichVert == cut2Vert)
//                             {
//                                 newViewerFace1.primFaceNumber = cut2FaceNumber;
//                                 newViewerFace2.primFaceNumber = cut2FaceNumber;
//                                 newViewerFace1.n1 = newLayer.cutNormal2;
//                                 newViewerFace1.n2 = lastCutNormal2;
//                                 newViewerFace1.n3 = lastCutNormal2;
//
//                                 newViewerFace2.n1 = newLayer.cutNormal2;
//                                 newViewerFace2.n3 = newLayer.cutNormal2;
//                                 newViewerFace2.n2 = lastCutNormal2;
//                             }
//
//                             else // outer and hollow faces
//                             {
//                                 if (sides < 5 && whichVert < newLayer.numOuterVerts ||
//                                     hollowSides < 5 && whichVert >= newLayer.numOuterVerts)
//                                 {
//                                     // looks terrible when path is twisted... need vertex normals here
//                                     newViewerFace1.CalcSurfaceNormal();
//                                     newViewerFace2.CalcSurfaceNormal();
//                                 }
//                                 else
//                                 {
//                                     newViewerFace1.n1 = normals[newFace1.n1];
//                                     newViewerFace1.n2 = normals[newFace1.n2];
//                                     newViewerFace1.n3 = normals[newFace1.n3];
//
//                                     newViewerFace2.n1 = normals[newFace2.n1];
//                                     newViewerFace2.n2 = normals[newFace2.n2];
//                                     newViewerFace2.n3 = normals[newFace2.n3];
//                                 }
//                             }
//
//                             viewerFaces.Add(newViewerFace1);
//                             viewerFaces.Add(newViewerFace2);
//                         }
//                     }
//                 }
//
//                 lastCutNormal1 = newLayer.cutNormal1;
//                 lastCutNormal2 = newLayer.cutNormal2;
//                 lastV = thisV;
//
//                 if (needEndFaces && nodeIndex == path.pathNodes.Count - 1 && viewerMode)
//                 {
//                     // add the top faces to the viewerFaces list here
//                     var faceNormal = newLayer.faceNormal;
//                     var newViewerFace = new ViewerFace(0);
//                     var numFaces = newLayer.faces.Count;
//                     var faces = newLayer.faces;
//
//                     for (var i = 0; i < numFaces; i++)
//                     {
//                         var face = faces[i];
//                         newViewerFace.v1 = newLayer.coords[face.v1 - coordsLen];
//                         newViewerFace.v2 = newLayer.coords[face.v2 - coordsLen];
//                         newViewerFace.v3 = newLayer.coords[face.v3 - coordsLen];
//
//                         newViewerFace.coordIndex1 = face.v1 - coordsLen;
//                         newViewerFace.coordIndex2 = face.v2 - coordsLen;
//                         newViewerFace.coordIndex3 = face.v3 - coordsLen;
//
//                         newViewerFace.n1 = faceNormal;
//                         newViewerFace.n2 = faceNormal;
//                         newViewerFace.n3 = faceNormal;
//
//                         newViewerFace.uv1 = newLayer.faceUVs[face.v1 - coordsLen];
//                         newViewerFace.uv2 = newLayer.faceUVs[face.v2 - coordsLen];
//                         newViewerFace.uv3 = newLayer.faceUVs[face.v3 - coordsLen];
//
//                         if (pathType == PathType.Linear)
//                         {
//                             newViewerFace.uv1.Flip();
//                             newViewerFace.uv2.Flip();
//                             newViewerFace.uv3.Flip();
//                         }
//
//                         viewerFaces.Add(newViewerFace);
//                     }
//                 }
//             } // for (int nodeIndex = 0; nodeIndex < path.pathNodes.Count; nodeIndex++)
//         }
//
//
//         /// <summary>
//         ///     DEPRICATED - use Extrude(PathType.Linear) instead
//         ///     Extrudes a profile along a straight line path. Used for prim types box, cylinder, and prism.
//         /// </summary>
//         public void ExtrudeLinear()
//         {
//             Extrude(PathType.Linear);
//         }
//
//
//         /// <summary>
//         ///     DEPRICATED - use Extrude(PathType.Circular) instead
//         ///     Extrude a profile into a circular path prim mesh. Used for prim types torus, tube, and ring.
//         /// </summary>
//         public void ExtrudeCircular()
//         {
//             Extrude(PathType.Circular);
//         }
//
//
//         private Coord SurfaceNormal(Coord c1, Coord c2, Coord c3)
//         {
//             var edge1 = new Coord(c2.X - c1.X, c2.Y - c1.Y, c2.Z - c1.Z);
//             var edge2 = new Coord(c3.X - c1.X, c3.Y - c1.Y, c3.Z - c1.Z);
//
//             var normal = Coord.Cross(edge1, edge2);
//
//             normal.Normalize();
//
//             return normal;
//         }
//
//         private Coord SurfaceNormal(Face face)
//         {
//             return SurfaceNormal(coords[face.v1], coords[face.v2], coords[face.v3]);
//         }
//
//         /// <summary>
//         ///     Calculate the surface normal for a face in the list of faces
//         /// </summary>
//         /// <param name="faceIndex"></param>
//         /// <returns></returns>
//         public Coord SurfaceNormal(int faceIndex)
//         {
//             var numFaces = faces.Count;
//             if (faceIndex < 0 || faceIndex >= numFaces)
//                 throw new Exception("faceIndex out of range");
//
//             return SurfaceNormal(faces[faceIndex]);
//         }
//
//         /// <summary>
//         ///     Duplicates a PrimMesh object. All object properties are copied by value, including lists.
//         /// </summary>
//         /// <returns></returns>
//         public PrimMesh Copy()
//         {
//             var copy = new PrimMesh(sides, profileStart, profileEnd, hollow, hollowSides)
//             {
//                 twistBegin = twistBegin,
//                 twistEnd = twistEnd,
//                 topShearX = topShearX,
//                 topShearY = topShearY,
//                 pathCutBegin = pathCutBegin,
//                 pathCutEnd = pathCutEnd,
//                 dimpleBegin = dimpleBegin,
//                 dimpleEnd = dimpleEnd,
//                 skew = skew,
//                 holeSizeX = holeSizeX,
//                 holeSizeY = holeSizeY,
//                 taperX = taperX,
//                 taperY = taperY,
//                 radius = radius,
//                 revolutions = revolutions,
//                 stepsPerRevolution = stepsPerRevolution,
//                 calcVertexNormals = calcVertexNormals,
//                 normalsProcessed = normalsProcessed,
//                 viewerMode = viewerMode,
//                 numPrimFaces = numPrimFaces,
//                 errorMessage = errorMessage,
//                 coords = new List<Coord>(coords),
//                 faces = new List<Face>(faces),
//                 viewerFaces = new List<ViewerFace>(viewerFaces),
//                 normals = new List<Coord>(normals)
//             };
//
//
//             return copy;
//         }
//
//         /// <summary>
//         ///     Calculate surface normals for all of the faces in the list of faces in this mesh
//         /// </summary>
//         public void CalcNormals()
//         {
//             if (normalsProcessed)
//                 return;
//
//             normalsProcessed = true;
//
//             var numFaces = faces.Count;
//
//             if (!calcVertexNormals)
//                 normals = new List<Coord>();
//
//             for (var i = 0; i < numFaces; i++)
//             {
//                 var face = faces[i];
//
//                 normals.Add(SurfaceNormal(i).Normalize());
//
//                 var normIndex = normals.Count - 1;
//                 face.n1 = normIndex;
//                 face.n2 = normIndex;
//                 face.n3 = normIndex;
//
//                 faces[i] = face;
//             }
//         }
//
//         /// <summary>
//         ///     Adds a value to each XYZ vertex coordinate in the mesh
//         /// </summary>
//         /// <param name="x"></param>
//         /// <param name="y"></param>
//         /// <param name="z"></param>
//         public void AddPos(float x, float y, float z)
//         {
//             int i;
//             var numVerts = coords.Count;
//             Coord vert;
//
//             for (i = 0; i < numVerts; i++)
//             {
//                 vert = coords[i];
//                 vert.X += x;
//                 vert.Y += y;
//                 vert.Z += z;
//                 coords[i] = vert;
//             }
//
//             if (viewerFaces != null)
//             {
//                 var numViewerFaces = viewerFaces.Count;
//
//                 for (i = 0; i < numViewerFaces; i++)
//                 {
//                     var v = viewerFaces[i];
//                     v.AddPos(x, y, z);
//                     viewerFaces[i] = v;
//                 }
//             }
//         }
//
//         /// <summary>
//         ///     Rotates the mesh
//         /// </summary>
//         /// <param name="q"></param>
//         public void AddRot(Quat q)
//         {
//             int i;
//             var numVerts = coords.Count;
//
//             for (i = 0; i < numVerts; i++)
//                 coords[i] *= q;
//
//             if (normals != null)
//             {
//                 var numNormals = normals.Count;
//                 for (i = 0; i < numNormals; i++)
//                     normals[i] *= q;
//             }
//
//             if (viewerFaces != null)
//             {
//                 var numViewerFaces = viewerFaces.Count;
//
//                 for (i = 0; i < numViewerFaces; i++)
//                 {
//                     var v = viewerFaces[i];
//                     v.v1 *= q;
//                     v.v2 *= q;
//                     v.v3 *= q;
//
//                     v.n1 *= q;
//                     v.n2 *= q;
//                     v.n3 *= q;
//                     viewerFaces[i] = v;
//                 }
//             }
//         }
//
// #if VERTEX_INDEXER
//         public VertexIndexer GetVertexIndexer()
//         {
//             if (viewerMode && viewerFaces.Count > 0)
//                 return new VertexIndexer(this);
//             return null;
//         }
// #endif
//
//         /// <summary>
//         ///     Scales the mesh
//         /// </summary>
//         /// <param name="x"></param>
//         /// <param name="y"></param>
//         /// <param name="z"></param>
//         public void Scale(float x, float y, float z)
//         {
//             int i;
//             var numVerts = coords.Count;
//             //Coord vert;
//
//             var m = new Coord(x, y, z);
//             for (i = 0; i < numVerts; i++)
//                 coords[i] *= m;
//
//             if (viewerFaces != null)
//             {
//                 var numViewerFaces = viewerFaces.Count;
//                 for (i = 0; i < numViewerFaces; i++)
//                 {
//                     var v = viewerFaces[i];
//                     v.v1 *= m;
//                     v.v2 *= m;
//                     v.v3 *= m;
//                     viewerFaces[i] = v;
//                 }
//             }
//         }
//
//         /// <summary>
//         ///     Dumps the mesh to a Blender compatible "Raw" format file
//         /// </summary>
//         /// <param name="path"></param>
//         /// <param name="name"></param>
//         /// <param name="title"></param>
//         public void DumpRaw(string path, string name, string title)
//         {
//             if (path == null)
//                 return;
//             var fileName = name + "_" + title + ".raw";
//             var completePath = System.IO.Path.Combine(path, fileName);
//             var sw = new StreamWriter(completePath);
//
//             for (var i = 0; i < faces.Count; i++)
//             {
//                 var s = coords[faces[i].v1].ToString();
//                 s += " " + coords[faces[i].v2];
//                 s += " " + coords[faces[i].v3];
//
//                 sw.WriteLine(s);
//             }
//
//             sw.Close();
//         }
//     }
//     
//     public class PrimMeshGenerator : IPrimMeshGenerator
//     {
//         const int ProfileCurveMask = 0x07;
//
//         private const int SquareSides = 4;
//         private const int TriSides = 4;
//         private const int CircleSides_LO = 6;
//         private const int CircleSides_MED = 12;
//         private const int CircleSides_HI = 24;
//         private const int Steps_LO = 6;
//         private const int Steps_MED = 12;
//         private const int Steps_HI = 24;
//         
//         private UMeshData GenerateMeshData(Primitive prim, DetailLevel LOD)
//         {
//             Primitive.ConstructionData primData = prim.PrimData;
//             int sides;
//             int hollowsides;
//
//             float profileBegin = primData.ProfileBegin;
//             float profileEnd = primData.ProfileEnd;
//
//             bool isSphere = false;
//             var profileCurve = (ProfileCurve)(primData.profileCurve & ProfileCurveMask);
//             sides = profileCurve switch
//             {
//                 ProfileCurve.Square => SquareSides,
//                 ProfileCurve.Circle => LOD switch
//                 {
//                     DetailLevel.Low => CircleSides_LO,
//                     DetailLevel.Medium => CircleSides_MED,
//                     DetailLevel.High => CircleSides_HI,
//                     DetailLevel.Highest => CircleSides_HI,
//                     _ => throw new ArgumentOutOfRangeException(nameof(LOD))
//                 },
//                 ProfileCurve.EqualTriangle => TriSides,
//                 ProfileCurve.HalfCircle => LOD switch
//                 {
//                     DetailLevel.Low => CircleSides_LO,
//                     DetailLevel.Medium => CircleSides_MED,
//                     DetailLevel.High => CircleSides_HI,
//                     DetailLevel.Highest => CircleSides_HI,
//                     _ => throw new ArgumentOutOfRangeException(nameof(LOD))
//                 },
//                 ProfileCurve.IsoTriangle => throw new NotImplementedException(),
//                 ProfileCurve.RightTriangle => throw new NotImplementedException(),
//                 _ => throw new NotImplementedException()
//             };
//
//             if (profileCurve == ProfileCurve.HalfCircle)
//             {
//                 // half circle, prim is a sphere
//                 isSphere = true;
//                 profileBegin = 0.5f * profileBegin + 0.5f;
//                 profileEnd = 0.5f * profileEnd + 0.5f;
//             }
//
//             hollowsides = primData.ProfileHole switch
//             {
//                 HoleType.Same => sides,
//                 HoleType.Circle => LOD switch
//                 {
//                     DetailLevel.Low => CircleSides_LO,
//                     DetailLevel.Medium => CircleSides_MED,
//                     DetailLevel.High => CircleSides_HI,
//                     DetailLevel.Highest => CircleSides_HI,
//                     _ => throw new ArgumentOutOfRangeException(nameof(LOD))
//                 },
//                 HoleType.Triangle => TriSides,
//                 HoleType.Square => SquareSides,
//                 _ => throw new ArgumentOutOfRangeException(nameof(primData.ProfileHole))
//             };
//             
//             PrimMesh newPrim =
//                 new PrimMesh(sides, profileBegin, profileEnd, primData.ProfileHollow, hollowsides)
//                 {
//                     viewerMode = viewerMode,
//                     sphereMode = isSphere,
//                     holeSizeX = primData.PathScaleX,
//                     holeSizeY = primData.PathScaleY,
//                     pathCutBegin = primData.PathBegin,
//                     pathCutEnd = primData.PathEnd,
//                     topShearX = primData.PathShearX,
//                     topShearY = primData.PathShearY,
//                     radius = primData.PathRadiusOffset,
//                     revolutions = primData.PathRevolutions,
//                     skew = primData.PathSkew
//                 };
//             newPrim.stepsPerRevolution = LOD switch
//             {
//                 DetailLevel.Low => Steps_LO,
//                 DetailLevel.Medium => Steps_MED,
//                 DetailLevel.High => Steps_HI,
//                 DetailLevel.Highest => Steps_HI,
//                 _ => throw new ArgumentOutOfRangeException(nameof(LOD))
//             };
//
//             if (primData.PathCurve == PathCurve.Line || primData.PathCurve == PathCurve.Flexible)
//             {
//                 newPrim.taperX = 1.0f - primData.PathScaleX;
//                 newPrim.taperY = 1.0f - primData.PathScaleY;
//                 newPrim.twistBegin = (int)(180 * primData.PathTwistBegin);
//                 newPrim.twistEnd = (int)(180 * primData.PathTwist);
//                 newPrim.ExtrudeLinear();
//             }
//             else
//             {
//                 newPrim.taperX = primData.PathTaperX;
//                 newPrim.taperY = primData.PathTaperY;
//                 newPrim.twistBegin = (int)(360 * primData.PathTwistBegin);
//                 newPrim.twistEnd = (int)(360 * primData.PathTwist);
//                 newPrim.ExtrudeCircular();
//             }
//
//             return newPrim;  
//         }
//     }
// }