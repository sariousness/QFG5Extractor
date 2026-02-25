using System;
using System.IO;

namespace QFG5Extractor.qfg5model
{
    public class qfg5modelMeshImport
    {
        public const int METHOD_1 = 0;
        public const int METHOD_2 = 1;
        public const int METHOD_3 = 2;
        public const int NUMBER_OF_METHODS = 3;

        public static void importHAKfile(string fileNameHakenberg, out float[] newsubmeshVerticesArray, out float[] newsubmeshFaceArray)
        {
            newsubmeshVerticesArray = null;
            newsubmeshFaceArray = null;

            using (StreamReader fileObjectHakenberg = File.OpenText(fileNameHakenberg))
            {
                bool readingVertices = false;
                bool readingTriangles = false;
                int numberOfVertices = 0;
                int numberOfTriangles = 0;
                int arrayIndex = 0;
                string currentLine = "";
                bool previousCharNewLine = false;

                while ((currentLine = fileObjectHakenberg.ReadLine()) != null)
                {
                    bool finishReadingArray = false;
                    if (currentLine == "")
                    {
                        if (previousCharNewLine)
                        {
                            finishReadingArray = true;
                        }
                        previousCharNewLine = true;
                    }
                    else if (currentLine[0] == '%')
                    {
                        // ignore commented line
                        finishReadingArray = true;
                    }
                    else if (currentLine[0] == 'v')
                    {
                        if (currentLine[2] == '3')
                        {
                            readingVertices = true;
                            string numberOfVerticesString = currentLine.Substring(4, currentLine.Length - 4);
                            numberOfVertices = Convert.ToInt32(numberOfVerticesString);
                            
                            int submeshVerticesArraySize = numberOfVertices * Constants.SUBMESH_DATAPOINTS_PER_VERTEX;
                            newsubmeshVerticesArray = new float[submeshVerticesArraySize]; // initialise array
                            arrayIndex = 0;
                        }
                        else
                        {
                            finishReadingArray = true;
                        }
                    }
                    else if (currentLine[0] == 'f')
                    {
                        if (currentLine[2] == '2')
                        {
                            readingTriangles = true;
                            string numberOfTrianglesString = currentLine.Substring(4, currentLine.Length - 4);
                            numberOfTriangles = Convert.ToInt32(numberOfTrianglesString);
                            
                            int submeshFacesArraySize = numberOfTriangles * Constants.SUBMESH_DATAPOINTS_PER_FACE;
                            newsubmeshFaceArray = new float[submeshFacesArraySize]; // initialise array
                            arrayIndex = 0;
                        }
                        else
                        {
                            finishReadingArray = true;
                        }
                    }
                    else
                    {
                        if (readingVertices)
                        {
                            arrayIndex = readLineOfFloats(currentLine, newsubmeshVerticesArray, Constants.SUBMESH_DATAPOINTS_PER_VERTEX, arrayIndex);
                        }
                        else if (readingTriangles)
                        {
                            arrayIndex = readLineOfFloats(currentLine, newsubmeshFaceArray, Constants.SUBMESH_DATAPOINTS_VERTICES_PER_FACE, arrayIndex);
                            arrayIndex = arrayIndex + (Constants.SUBMESH_DATAPOINTS_PER_FACE - Constants.SUBMESH_DATAPOINTS_VERTICES_PER_FACE); // skip UV coords and normals for now...
                        }
                    }

                    if (finishReadingArray)
                    {
                        readingVertices = false;
                        readingTriangles = false;
                        arrayIndex = 0;
                    }

                    if (currentLine != "")
                    {
                        previousCharNewLine = false;
                    }
                }
            }
        }

        private static int readLineOfFloats(string currentLine, float[] floatArray, int numberOfFloats, int arrayIndex)
        {
            int startOfdataPoint = 0;
            int endOfdataPoint = -1;

            for (int j = 0; j < numberOfFloats; j++)
            {
                startOfdataPoint = endOfdataPoint + 1;
                endOfdataPoint = currentLine.IndexOf(' ', startOfdataPoint);
                string dataPointString;
                
                if (endOfdataPoint == -1)
                {
                    dataPointString = currentLine.Substring(startOfdataPoint);
                }
                else
                {
                    dataPointString = currentLine.Substring(startOfdataPoint, endOfdataPoint - startOfdataPoint);
                }

                float dataPoint;
                if (dataPointString == "-1.#IND")
                {
                    Console.WriteLine("HAK_BAD_VERTEX detected");
                    dataPoint = 1.0f;
                }
                else
                {
                    dataPoint = Convert.ToSingle(dataPointString);
                }

                floatArray[arrayIndex] = dataPoint;
                arrayIndex++;
            }
            return arrayIndex;
        }

        public static void formatNew3DmeshData(string submeshName, float[] submeshVerticesArray, float[] submeshUVcoordsArray, float[] submeshFaceArray, int[] submeshLightingEntriesArray, float[] newsubmeshVerticesArray, out float[] newsubmeshUVcoordsArray, float[] newsubmeshFaceArray, out int[] newsubmeshLightingEntriesArray)
        {
            poly[] submeshVerticesPolyArray = new poly[submeshFaceArray.Length / Constants.SUBMESH_DATAPOINTS_PER_FACE];
            poly[] newsubmeshVerticesPolyArray = new poly[newsubmeshFaceArray.Length / Constants.SUBMESH_DATAPOINTS_PER_FACE];
            poly[] submeshVerticesUVcoordsPolyArray = new poly[submeshFaceArray.Length / Constants.SUBMESH_DATAPOINTS_PER_FACE];
            
            qfg5modelMath.fillSubmeshVerticesPolygonArray(submeshFaceArray, submeshVerticesArray, submeshVerticesPolyArray);
            qfg5modelMath.fillSubmeshVerticesPolygonArray(newsubmeshFaceArray, newsubmeshVerticesArray, newsubmeshVerticesPolyArray);
            qfg5modelMath.fillSubmeshUVcoordsPolygonArray(submeshFaceArray, submeshUVcoordsArray, submeshVerticesUVcoordsPolyArray);

            vec[] submeshUVmappingVerticesArray = new vec[submeshVerticesArray.Length / Constants.SUBMESH_DATAPOINTS_PER_VERTEX];
            poly[] submeshUVmappingPolyArray = new poly[submeshVerticesPolyArray.Length];
            int[] newsubmeshPolyArrayIndexOfClosestsubmeshUVmappingPoly = new int[newsubmeshVerticesPolyArray.Length]; 
            int[] newsubmeshPolyArrayIndexOfClosestsubmeshUVmappingPolyMethod = new int[newsubmeshVerticesPolyArray.Length]; 
            
            bool[] submeshUVmappingPolyArrayFoundIdenticalPnew = new bool[submeshUVmappingPolyArray.Length]; 
            bool[] newsubmeshPolyArrayFoundIdenticalPorig = new bool[newsubmeshVerticesPolyArray.Length]; 
            
            vec[] submeshUVmappingPolyArrayNormals = new vec[submeshUVmappingPolyArray.Length];

            int newsubmeshUVcoordsArrayMaximumSize = submeshUVcoordsArray.Length + (newsubmeshVerticesPolyArray.Length * Constants.SUBMESH_DATAPOINTS_PER_UV_COORDS * 3); 
            int newsubmeshUVcoordsArrayNewDataIndex = submeshUVcoordsArray.Length;
            float[] newsubmeshUVcoordsArrayTemp = new float[newsubmeshUVcoordsArrayMaximumSize];
            
            for (int i = 0; i < submeshUVcoordsArray.Length; i++)
            {
                newsubmeshUVcoordsArrayTemp[i] = submeshUVcoordsArray[i];
            }

            int unknownDataValue = -1;
            bool unknownDataValueFound = false;
            for (int i = 0; i < submeshFaceArray.Length; i++)
            {
                int faceInternalDataPointIndex = i % Constants.SUBMESH_DATAPOINTS_PER_FACE;
                if (faceInternalDataPointIndex == Constants.SUBMESH_DATAPOINTS_FACE_INDEX_OF_UNKNOWN)
                {
                    if (unknownDataValueFound)
                    {
                        if (unknownDataValue != submeshFaceArray[i])
                        {
                            Logger.LogWarning("unknownDataValue != submeshFaceArray[i]; a submesh utilises more than one subBitmap");
                        }
                    }
                    else
                    {
                        unknownDataValue = (int)Math.Round(submeshFaceArray[i]);
                        unknownDataValueFound = true;
                    }
                }
            }

            int newnumberOfVertices = newsubmeshVerticesArray.Length / Constants.SUBMESH_DATAPOINTS_PER_VERTEX;
            int numberOfVertices = submeshVerticesArray.Length / Constants.SUBMESH_DATAPOINTS_PER_VERTEX;
            
            newsubmeshLightingEntriesArray = new int[newnumberOfVertices * Constants.SUBMESH_NUMBER_OF_LIGHTING_ENTRIES_PER_VERTEX];

            for (int iNew = 0; iNew < newnumberOfVertices; iNew++)
            {
                int matchingVertexFoundIndexi = 0;
                double minDistance = Constants.SOME_LARGE_DISTANCE;
                for (int i = 0; i < numberOfVertices; i++)
                {
                    vec P1 = new vec();
                    P1.x = submeshVerticesArray[i * Constants.SUBMESH_DATAPOINTS_PER_VERTEX + 0];
                    P1.y = submeshVerticesArray[i * Constants.SUBMESH_DATAPOINTS_PER_VERTEX + 1];
                    P1.z = submeshVerticesArray[i * Constants.SUBMESH_DATAPOINTS_PER_VERTEX + 2];
                    vec P2 = new vec();
                    P2.x = newsubmeshVerticesArray[iNew * Constants.SUBMESH_DATAPOINTS_PER_VERTEX + 0];
                    P2.y = newsubmeshVerticesArray[iNew * Constants.SUBMESH_DATAPOINTS_PER_VERTEX + 1];
                    P2.z = newsubmeshVerticesArray[iNew * Constants.SUBMESH_DATAPOINTS_PER_VERTEX + 2];

                    double distance = qfg5modelMath.calculateTheDistanceBetweenTwoPoints(P1, P2);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        matchingVertexFoundIndexi = i;
                    }
                }

                for (int d = 0; d < Constants.SUBMESH_DATAPOINTS_PER_LIGHTING_ENTRY; d++)
                {
                    newsubmeshLightingEntriesArray[iNew * Constants.SUBMESH_DATAPOINTS_PER_LIGHTING_ENTRY + d] = submeshLightingEntriesArray[matchingVertexFoundIndexi * Constants.SUBMESH_DATAPOINTS_PER_LIGHTING_ENTRY + d];
                }
            }

            for (int p = 0; p < submeshVerticesPolyArray.Length; p++)
            {
                submeshUVmappingPolyArrayFoundIdenticalPnew[p] = false;
            }
            for (int pNew = 0; pNew < newsubmeshVerticesPolyArray.Length; pNew++)
            {
                newsubmeshPolyArrayFoundIdenticalPorig[pNew] = false;
            }
            for (int p = 0; p < submeshVerticesPolyArray.Length; p++)
            {
                for (int pNew = 0; pNew < newsubmeshVerticesPolyArray.Length; pNew++)
                {
                    bool[] vertexMatchArray = new bool[3] { false, false, false };

                    for (int v = 0; v < qfg5modelMath.NUM_VERTICES_PER_POLYGON; v++)
                    {
                        vec vertex = qfg5modelMath.getPolyVertex(submeshVerticesPolyArray[p], v);
                        vec vertexNew = qfg5modelMath.getPolyVertex(newsubmeshVerticesPolyArray[pNew], v);
                        if (qfg5modelMath.compareVectors(vertex, vertexNew))
                        {
                            vertexMatchArray[v] = true;
                        }
                    }
                    if (vertexMatchArray[0] && vertexMatchArray[1] && vertexMatchArray[2])
                    {
                        submeshUVmappingPolyArray[p] = newsubmeshVerticesPolyArray[pNew];
                        newsubmeshPolyArrayIndexOfClosestsubmeshUVmappingPoly[pNew] = p;
                        submeshUVmappingPolyArrayFoundIdenticalPnew[p] = true;
                        newsubmeshPolyArrayFoundIdenticalPorig[pNew] = true;
                    }
                }
            }

            for (int i = 0; i < submeshVerticesArray.Length; i += Constants.SUBMESH_DATAPOINTS_PER_VERTEX)
            {
                vec vertex = new vec();
                vertex.x = submeshVerticesArray[i + 0];
                vertex.y = submeshVerticesArray[i + 1];
                vertex.z = submeshVerticesArray[i + 2];

                double minDistance = Constants.SOME_LARGE_DISTANCE;
                vec vertexNewAtMinDistance = new vec();
                for (int iNew = 0; iNew < newsubmeshVerticesArray.Length; iNew += Constants.SUBMESH_DATAPOINTS_PER_VERTEX)
                {
                    vec vertexNew = new vec();
                    vertexNew.x = newsubmeshVerticesArray[iNew + 0];
                    vertexNew.y = newsubmeshVerticesArray[iNew + 1];
                    vertexNew.z = newsubmeshVerticesArray[iNew + 2];
                    double distance = qfg5modelMath.calculateTheDistanceBetweenTwoPoints(vertex, vertexNew);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        vertexNewAtMinDistance = vertexNew;
                    }
                }
                submeshUVmappingVerticesArray[i / Constants.SUBMESH_DATAPOINTS_PER_VERTEX] = vertexNewAtMinDistance;
            }

            for (int p = 0; p < submeshVerticesPolyArray.Length; p++)
            {
                for (int v = 0; v < qfg5modelMath.NUM_VERTICES_PER_POLYGON; v++)
                {
                    vec vertex = qfg5modelMath.getPolyVertex(submeshVerticesPolyArray[p], v);
                    bool foundMatch = false;
                    for (int i = 0; i < submeshVerticesArray.Length; i += Constants.SUBMESH_DATAPOINTS_PER_VERTEX)
                    {
                        vec vertextest = new vec();
                        vertextest.x = submeshVerticesArray[i + 0];
                        vertextest.y = submeshVerticesArray[i + 1];
                        vertextest.z = submeshVerticesArray[i + 2];
                        if (qfg5modelMath.compareVectors(vertextest, vertex))
                        {
                            foundMatch = true;
                            qfg5modelMath.setPolyVertex(ref submeshUVmappingPolyArray[p], v, submeshUVmappingVerticesArray[i / Constants.SUBMESH_DATAPOINTS_PER_VERTEX]);
                        }
                    }

                    if (!foundMatch)
                    {
                        Console.WriteLine("error: = !foundMatch");
                    }
                }
            }

            for (int pNew = 0; pNew < newsubmeshVerticesPolyArray.Length; pNew++)
            {
                poly newsubmeshPoly = newsubmeshVerticesPolyArray[pNew];
                vec vertexpNewCentre = qfg5modelMath.calculateCentreOfPolygon(newsubmeshVerticesPolyArray[pNew]);

                if (!newsubmeshPolyArrayFoundIdenticalPorig[pNew])
                {
                    int[] pAtMinDistance = new int[NUMBER_OF_METHODS] { 0, 0, 0 };
                    double[] minDistance = new double[NUMBER_OF_METHODS] { Constants.SOME_LARGE_DISTANCE, Constants.SOME_LARGE_DISTANCE, Constants.SOME_LARGE_DISTANCE };
                    bool[] foundClosestUVmappingPoly = new bool[NUMBER_OF_METHODS] { false, false, false };

                    for (int p = 0; p < submeshVerticesPolyArray.Length; p++)
                    {
                        poly submeshUVmappingPoly = submeshUVmappingPolyArray[p];
                        for (int method = 0; method <= METHOD_2; method++)
                        {
                            vec N;
                            if (method == METHOD_1)
                            {
                                N = qfg5modelMath.calculateNormalOfPoly(submeshUVmappingPoly);
                            }
                            else 
                            {
                                N = qfg5modelMath.calculateNormalOfPoly(newsubmeshPoly);
                            }
                            N = qfg5modelMath.multiplyVectorByScalar(N, 100.0);

                            bool foundIntesectionPoint = false;
                            vec P1 = vertexpNewCentre;
                            vec P2 = qfg5modelMath.addVectors(P1, N);
                            vec intersectionPointWithsubmeshUVmappingPoly = qfg5modelMath.calculateIntersectionPointOfLineAndPolygonPlane(P1, P2, submeshUVmappingPoly, ref foundIntesectionPoint);

                            if (foundIntesectionPoint)
                            {
                                double distance = qfg5modelMath.calculateTheDistanceBetweenTwoPoints(intersectionPointWithsubmeshUVmappingPoly, vertexpNewCentre);

                                if (distance < minDistance[method])
                                {
                                    if (qfg5modelMath.checkIfPointLiesOnTriangle3D(submeshUVmappingPoly, intersectionPointWithsubmeshUVmappingPoly))
                                    {
                                        minDistance[method] = distance;
                                        pAtMinDistance[method] = p;
                                        foundClosestUVmappingPoly[method] = true;
                                    }
                                }
                            }
                        }

                        vec vertexpCentre = qfg5modelMath.calculateCentreOfPolygon(submeshUVmappingPoly);
                        double distance3 = qfg5modelMath.calculateTheDistanceBetweenTwoPoints(vertexpCentre, vertexpNewCentre);
                        if (distance3 < minDistance[METHOD_3])
                        {
                            minDistance[METHOD_3] = distance3;
                            pAtMinDistance[METHOD_3] = p;
                            foundClosestUVmappingPoly[METHOD_3] = true;
                        }
                    }

                    if (foundClosestUVmappingPoly[METHOD_2])
                    {
                        newsubmeshPolyArrayIndexOfClosestsubmeshUVmappingPoly[pNew] = pAtMinDistance[METHOD_2];
                        newsubmeshPolyArrayIndexOfClosestsubmeshUVmappingPolyMethod[pNew] = METHOD_2;
                    }
                    else if (foundClosestUVmappingPoly[METHOD_1])
                    {
                        newsubmeshPolyArrayIndexOfClosestsubmeshUVmappingPoly[pNew] = pAtMinDistance[METHOD_1];
                        newsubmeshPolyArrayIndexOfClosestsubmeshUVmappingPolyMethod[pNew] = METHOD_1;
                    }
                    else
                    {
                        Logger.LogWarning("!foundClosestUVmappingPoly during closest mesh mapping step.");
                    }
                }
            }

            for (int p = 0; p < submeshVerticesPolyArray.Length; p++)
            {
                vec normal;
                if (submeshUVmappingPolyArrayFoundIdenticalPnew[p])
                {
                    normal = new vec();
                    for (int j = 0; j < Constants.SUBMESH_DATAPOINTS_PER_NORMAL; j++)
                    {
                        float value = submeshFaceArray[p * Constants.SUBMESH_DATAPOINTS_PER_FACE + Constants.SUBMESH_DATAPOINTS_FACE_INDEX_OF_NORMAL + j];
                        qfg5modelMath.setVectorVal(ref normal, j, value);
                    }
                }
                else
                {
                    normal = qfg5modelMath.calculateNormalOfPoly(submeshUVmappingPolyArray[p]);
                }
                submeshUVmappingPolyArrayNormals[p] = normal;
            }

            for (int pNew = 0; pNew < newsubmeshVerticesPolyArray.Length; pNew++)
            {
                poly newsubmeshPoly = newsubmeshVerticesPolyArray[pNew];
                for (int vNew = 0; vNew < qfg5modelMath.NUM_VERTICES_PER_POLYGON; vNew++)
                {
                    vec vertexNew = qfg5modelMath.getPolyVertex(newsubmeshVerticesPolyArray[pNew], vNew);
                    bool newsubmeshVectorIsInsubmeshUVmappingPolyArray = false;
                    int pAtMatch = 0;
                    int vAtMatch = 0;

                    int p = newsubmeshPolyArrayIndexOfClosestsubmeshUVmappingPoly[pNew];
                    for (int v = 0; v < qfg5modelMath.NUM_VERTICES_PER_POLYGON; v++)
                    {
                        vec vertex = qfg5modelMath.getPolyVertex(submeshUVmappingPolyArray[p], v);
                        if (qfg5modelMath.compareVectors(vertex, vertexNew))
                        {
                            newsubmeshVectorIsInsubmeshUVmappingPolyArray = true;
                            pAtMatch = p;
                            vAtMatch = v;
                        }
                    }

                    if (newsubmeshVectorIsInsubmeshUVmappingPolyArray)
                    {
                        newsubmeshFaceArray[pNew * Constants.SUBMESH_DATAPOINTS_PER_FACE + Constants.SUBMESH_DATAPOINTS_FACE_INDEX_OF_FIRST_UVCOORD + vNew] = submeshFaceArray[pAtMatch * Constants.SUBMESH_DATAPOINTS_PER_FACE + Constants.SUBMESH_DATAPOINTS_FACE_INDEX_OF_FIRST_UVCOORD + vAtMatch];
                        newsubmeshFaceArray[pNew * Constants.SUBMESH_DATAPOINTS_PER_FACE + Constants.SUBMESH_DATAPOINTS_FACE_INDEX_OF_UNKNOWN] = submeshFaceArray[pAtMatch * Constants.SUBMESH_DATAPOINTS_PER_FACE + Constants.SUBMESH_DATAPOINTS_FACE_INDEX_OF_UNKNOWN];
                    }
                    else
                    {
                        int pLookup = newsubmeshPolyArrayIndexOfClosestsubmeshUVmappingPoly[pNew];
                        bool closestIntersectionXPointFound = false;

                        vec N;
                        int method = newsubmeshPolyArrayIndexOfClosestsubmeshUVmappingPolyMethod[pNew];

                        if (method == METHOD_1)
                        {
                            N = submeshUVmappingPolyArrayNormals[pLookup];
                        }
                        else
                        {
                            N = qfg5modelMath.calculateNormalOfPoly(newsubmeshPoly);
                        }
                        N = qfg5modelMath.multiplyVectorByScalar(N, 100.0);

                        vec P1 = qfg5modelMath.getPolyVertex(newsubmeshVerticesPolyArray[pNew], vNew);
                        vec P2 = qfg5modelMath.addVectors(P1, N);
                        vec closestIntersectionXPoint = qfg5modelMath.calculateIntersectionPointOfLineAndPolygonPlane(P1, P2, submeshUVmappingPolyArray[pLookup], ref closestIntersectionXPointFound);

                        if (closestIntersectionXPointFound)
                        {
                            vec submeshUVmappingPolyUVvaluesVertexA = qfg5modelMath.getPolyVertex(submeshVerticesUVcoordsPolyArray[pLookup], 0);
                            vec submeshUVmappingPolyUVvaluesVertexB = qfg5modelMath.getPolyVertex(submeshVerticesUVcoordsPolyArray[pLookup], 1);
                            vec submeshUVmappingPolyUVvaluesVertexC = qfg5modelMath.getPolyVertex(submeshVerticesUVcoordsPolyArray[pLookup], 2);

                            vec A = submeshUVmappingPolyArray[pLookup].a;
                            vec B = submeshUVmappingPolyArray[pLookup].b;
                            vec C = submeshUVmappingPolyArray[pLookup].c;

                            bool baryCentricPass = false;
                            vec idealUVvaluesBasedOnLinearInterpolation = qfg5modelMath.mapPointFromTriangle1toTriangle2(submeshUVmappingPolyArray[pLookup], closestIntersectionXPoint, submeshVerticesUVcoordsPolyArray[pLookup], ref baryCentricPass);
                            
                            if (baryCentricPass)
                            {
                                bool isExistingNewUVcoord = false;
                                int existingNewUVcoordIndex = 0;
                                for (int i = 0; i < newsubmeshUVcoordsArrayNewDataIndex; i += Constants.SUBMESH_DATAPOINTS_PER_UV_COORDS)
                                {
                                    vec existingNewUVcoord = new vec();
                                    existingNewUVcoord.x = newsubmeshUVcoordsArrayTemp[i];
                                    existingNewUVcoord.y = newsubmeshUVcoordsArrayTemp[i + 1];
                                    existingNewUVcoord.z = 0.0f;
                                    idealUVvaluesBasedOnLinearInterpolation.z = 0.0f;
                                    if (qfg5modelMath.compareVectors(existingNewUVcoord, idealUVvaluesBasedOnLinearInterpolation))
                                    {
                                        isExistingNewUVcoord = true;
                                        existingNewUVcoordIndex = i;
                                    }
                                }

                                if (isExistingNewUVcoord)
                                {
                                    newsubmeshFaceArray[pNew * Constants.SUBMESH_DATAPOINTS_PER_FACE + Constants.SUBMESH_DATAPOINTS_FACE_INDEX_OF_FIRST_UVCOORD + vNew] = (float)(existingNewUVcoordIndex / Constants.SUBMESH_DATAPOINTS_PER_UV_COORDS);
                                }
                                else
                                {
                                    newsubmeshUVcoordsArrayTemp[newsubmeshUVcoordsArrayNewDataIndex + 0] = idealUVvaluesBasedOnLinearInterpolation.x;
                                    newsubmeshUVcoordsArrayTemp[newsubmeshUVcoordsArrayNewDataIndex + 1] = idealUVvaluesBasedOnLinearInterpolation.y;
                                    newsubmeshFaceArray[pNew * Constants.SUBMESH_DATAPOINTS_PER_FACE + Constants.SUBMESH_DATAPOINTS_FACE_INDEX_OF_FIRST_UVCOORD + vNew] = (float)(newsubmeshUVcoordsArrayNewDataIndex / Constants.SUBMESH_DATAPOINTS_PER_UV_COORDS);
                                    newsubmeshUVcoordsArrayNewDataIndex += Constants.SUBMESH_DATAPOINTS_PER_UV_COORDS;
                                }
                                newsubmeshFaceArray[pNew * Constants.SUBMESH_DATAPOINTS_PER_FACE + Constants.SUBMESH_DATAPOINTS_FACE_INDEX_OF_UNKNOWN] = unknownDataValue;
                            }
                            else
                            {
                                Logger.LogWarning("!baryCentricPass out of bounds mapping on triangle interpolation.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("0 error: !closestIntersectionXPointFound");
                        }
                    }
                }
            }

            for (int pNew = 0; pNew < newsubmeshVerticesPolyArray.Length; pNew++)
            {
                if (newsubmeshPolyArrayFoundIdenticalPorig[pNew])
                {
                    int indexOfIdenticalsubmeshUVmappingPoly = newsubmeshPolyArrayIndexOfClosestsubmeshUVmappingPoly[pNew];
                    newsubmeshFaceArray[pNew * Constants.SUBMESH_DATAPOINTS_PER_FACE + Constants.SUBMESH_DATAPOINTS_FACE_INDEX_OF_NORMAL + 0] = submeshFaceArray[indexOfIdenticalsubmeshUVmappingPoly * Constants.SUBMESH_DATAPOINTS_PER_FACE + Constants.SUBMESH_DATAPOINTS_FACE_INDEX_OF_NORMAL + 0];
                    newsubmeshFaceArray[pNew * Constants.SUBMESH_DATAPOINTS_PER_FACE + Constants.SUBMESH_DATAPOINTS_FACE_INDEX_OF_NORMAL + 1] = submeshFaceArray[indexOfIdenticalsubmeshUVmappingPoly * Constants.SUBMESH_DATAPOINTS_PER_FACE + Constants.SUBMESH_DATAPOINTS_FACE_INDEX_OF_NORMAL + 1];
                    newsubmeshFaceArray[pNew * Constants.SUBMESH_DATAPOINTS_PER_FACE + Constants.SUBMESH_DATAPOINTS_FACE_INDEX_OF_NORMAL + 2] = submeshFaceArray[indexOfIdenticalsubmeshUVmappingPoly * Constants.SUBMESH_DATAPOINTS_PER_FACE + Constants.SUBMESH_DATAPOINTS_FACE_INDEX_OF_NORMAL + 2];
                }
                else
                {
                    vec normal = qfg5modelMath.calculateNormalOfPoly(newsubmeshVerticesPolyArray[pNew]);
                    newsubmeshFaceArray[pNew * Constants.SUBMESH_DATAPOINTS_PER_FACE + Constants.SUBMESH_DATAPOINTS_FACE_INDEX_OF_NORMAL + 0] = normal.x;
                    newsubmeshFaceArray[pNew * Constants.SUBMESH_DATAPOINTS_PER_FACE + Constants.SUBMESH_DATAPOINTS_FACE_INDEX_OF_NORMAL + 1] = normal.y;
                    newsubmeshFaceArray[pNew * Constants.SUBMESH_DATAPOINTS_PER_FACE + Constants.SUBMESH_DATAPOINTS_FACE_INDEX_OF_NORMAL + 2] = normal.z;
                }
            }

            newsubmeshUVcoordsArray = new float[newsubmeshUVcoordsArrayNewDataIndex];
            for (int i = 0; i < newsubmeshUVcoordsArrayNewDataIndex; i++)
            {
                newsubmeshUVcoordsArray[i] = newsubmeshUVcoordsArrayTemp[i];
            }
        }
    }
}
