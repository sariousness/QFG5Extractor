using System;

namespace QFG5Extractor.qfg5model
{
    public struct vec
    {
        public float x, y, z;
    }

    public struct mat
    {
        public vec a, b, c;
    }

    public struct poly
    {
        public vec a, b, c;
    }

    public static class qfg5modelMath
    {
        public const float PI = 3.14159265f;
        public const int NUM_VERTICES_PER_POLYGON = 3;
        public const int NUM_DATAPOINTS_PER_VERTEX = 3;

        public static void printValue(string title, float val)
        {
            Console.WriteLine("{0} = {1}", title, val);
        }

        public static void printVector(string title, vec vect)
        {
            Console.WriteLine("Vector {0} = {1} {2} {3}", title, vect.x, vect.y, vect.z);
        }

        public static void printPoly(string title, poly pol)
        {
            Console.WriteLine("Poly: {0} = ", title);
            printVector("pol.a", pol.a);
            printVector("pol.b", pol.b);
            printVector("pol.c", pol.c);
        }

        public static bool compareVectorsArbitraryError(vec vect1, vec vect2, double error)
        {
            if (!compareDoublesArbitraryError(vect1.x, vect2.x, error)) return false;
            if (!compareDoublesArbitraryError(vect1.y, vect2.y, error)) return false;
            if (!compareDoublesArbitraryError(vect1.z, vect2.z, error)) return false;
            return true;
        }

        public static bool compareVectors(vec vect1, vec vect2)
        {
            if (!compareDoubles(vect1.x, vect2.x)) return false;
            if (!compareDoubles(vect1.y, vect2.y)) return false;
            if (!compareDoubles(vect1.z, vect2.z)) return false;
            return true;
        }

        public static void copyVector(ref vec vecNew, vec vecbToCopy)
        {
            vecNew.x = vecbToCopy.x;
            vecNew.y = vecbToCopy.y;
            vecNew.z = vecbToCopy.z;
        }

        public static void setVectorVal(ref vec vect, int j, float value)
        {
            if (j == 0) vect.x = value;
            else if (j == 1) vect.y = value;
            else if (j == 2) vect.z = value;
        }

        public static float getVectorVal(vec vect, int j)
        {
            if (j == 0) return vect.x;
            if (j == 1) return vect.y;
            if (j == 2) return vect.z;
            return 0;
        }

        public static void setPolyVertex(ref poly pol, int v, vec value)
        {
            if (v == 0) pol.a = value;
            else if (v == 1) pol.b = value;
            else if (v == 2) pol.c = value;
        }

        public static vec getPolyVertex(poly pol, int v)
        {
            if (v == 0) return pol.a;
            if (v == 1) return pol.b;
            if (v == 2) return pol.c;
            return new vec();
        }

        public static void setPolygonPoint(ref poly pol, int v, int j, float value)
        {
            if (v == 0)
            {
                if (j == 0) pol.a.x = value;
                else if (j == 1) pol.a.y = value;
                else if (j == 2) pol.a.z = value;
            }
            else if (v == 1)
            {
                if (j == 0) pol.b.x = value;
                else if (j == 1) pol.b.y = value;
                else if (j == 2) pol.b.z = value;
            }
            else if (v == 2)
            {
                if (j == 0) pol.c.x = value;
                else if (j == 1) pol.c.y = value;
                else if (j == 2) pol.c.z = value;
            }
        }

        public static float getPolygonPoint(poly pol, int v, int j)
        {
            if (v == 0)
            {
                if (j == 0) return pol.a.x;
                if (j == 1) return pol.a.y;
                if (j == 2) return pol.a.z;
            }
            else if (v == 1)
            {
                if (j == 0) return pol.b.x;
                if (j == 1) return pol.b.y;
                if (j == 2) return pol.b.z;
            }
            else if (v == 2)
            {
                if (j == 0) return pol.c.x;
                if (j == 1) return pol.c.y;
                if (j == 2) return pol.c.z;
            }
            return 0;
        }

        public static void fillSubmeshVerticesPolygonArray(float[] submeshFaceArray, float[] submeshVerticesArray, poly[] submeshVerticesPolyArray)
        {
            for (int f = 0; f < submeshFaceArray.Length; f += Constants.SUBMESH_DATAPOINTS_PER_FACE)
            {
                for (int v = Constants.SUBMESH_DATAPOINTS_FACE_INDEX_OF_FIRST_VERTEX; v < Constants.SUBMESH_DATAPOINTS_VERTICES_PER_FACE; v++)
                {
                    int vertexIndex = (int)submeshFaceArray[f + v];
                    for (int j = 0; j < Constants.SUBMESH_DATAPOINTS_PER_VERTEX; j++)
                    {
                        float dataPoint = submeshVerticesArray[vertexIndex * Constants.SUBMESH_DATAPOINTS_PER_VERTEX + j];
                        poly p = submeshVerticesPolyArray[f / Constants.SUBMESH_DATAPOINTS_PER_FACE];
                        setPolygonPoint(ref p, v, j, dataPoint);
                        submeshVerticesPolyArray[f / Constants.SUBMESH_DATAPOINTS_PER_FACE] = p;
                    }
                }
            }
        }

        public static void fillSubmeshUVcoordsPolygonArray(float[] submeshFaceArray, float[] submeshUVcoordsArray, poly[] submeshUVcoordsPolyArray)
        {
            for (int f = 0; f < submeshFaceArray.Length; f += Constants.SUBMESH_DATAPOINTS_PER_FACE)
            {
                for (int v = 0; v < Constants.SUBMESH_DATAPOINTS_UVCOORDS_PER_FACE; v++)
                {
                    int vIndexInFace = v + Constants.SUBMESH_DATAPOINTS_FACE_INDEX_OF_FIRST_UVCOORD;
                    int vertexIndex = (int)submeshFaceArray[f + vIndexInFace];
                    poly p = submeshUVcoordsPolyArray[f / Constants.SUBMESH_DATAPOINTS_PER_FACE];
                    for (int j = 0; j < Constants.SUBMESH_DATAPOINTS_PER_UV_COORDS; j++)
                    {
                        float dataPoint = submeshUVcoordsArray[vertexIndex * Constants.SUBMESH_DATAPOINTS_PER_UV_COORDS + j];
                        setPolygonPoint(ref p, v, j, dataPoint);
                    }
                    setPolygonPoint(ref p, v, Constants.SUBMESH_DATAPOINTS_PER_UV_COORDS, 0.0f); // using x(U), y(V) coords only (set z coordinate to zero)
                    submeshUVcoordsPolyArray[f / Constants.SUBMESH_DATAPOINTS_PER_FACE] = p;
                }
            }
        }

        public static vec calculateCentreOfPolygon(poly pol)
        {
            vec averageValues = new vec();
            for (int v = 0; v < NUM_VERTICES_PER_POLYGON; v++)
            {
                for (int j = 0; j < NUM_DATAPOINTS_PER_VERTEX; j++)
                {
                    float currentAveragedVal = getVectorVal(averageValues, j);
                    float dataPoint = getPolygonPoint(pol, v, j);
                    setVectorVal(ref averageValues, j, currentAveragedVal + dataPoint);
                }
            }
            for (int j = 0; j < NUM_DATAPOINTS_PER_VERTEX; j++)
            {
                float currentAveragedVal = getVectorVal(averageValues, j);
                setVectorVal(ref averageValues, j, currentAveragedVal / Constants.SUBMESH_DATAPOINTS_PER_VERTEX);
            }
            return averageValues;
        }

        public static vec calculateNormalOfPoly(poly pol)
        {
            vec vec1 = subtractVectors(pol.b, pol.a);
            vec vec2 = subtractVectors(pol.c, pol.a);
            return calculateNormal(vec1, vec2);
        }

        public static vec calculateNormal(vec pt1, vec pt2)
        {
            return crossProduct(pt1, pt2);
        }

        public static vec crossProduct(vec vect1, vec vect2)
        {
            vec vect = new vec();
            vect.x = (vect1.y) * (vect2.z) - (vect2.y) * (vect1.z);
            vect.y = -((vect1.x) * (vect2.z) - (vect2.x) * (vect1.z));
            vect.z = (vect1.x) * (vect2.y) - (vect2.x) * (vect1.y);
            return vect;
        }

        public static vec subtractVectors(vec vect1, vec vect2)
        {
            vec vect = new vec();
            vect.x = vect1.x - vect2.x;
            vect.y = vect1.y - vect2.y;
            vect.z = vect1.z - vect2.z;
            return vect;
        }

        public static vec addVectors(vec vect1, vec vect2)
        {
            vec vect = new vec();
            vect.x = vect1.x + vect2.x;
            vect.y = vect1.y + vect2.y;
            vect.z = vect1.z + vect2.z;
            return vect;
        }

        public static double dotProduct(vec vect1, vec vect2)
        {
            return vect1.x * vect2.x + vect1.y * vect2.y + vect1.z * vect2.z;
        }

        public static vec multiplyVectorByScalar(vec vect1, double scalar)
        {
            vec vect = new vec();
            vect.x = (float)(vect1.x * scalar);
            vect.y = (float)(vect1.y * scalar);
            vect.z = (float)(vect1.z * scalar);
            return vect;
        }

        public static vec divideVectorByScalar(vec vect1, double divisor)
        {
            vec vect = new vec();
            vect.x = (float)(vect1.x / divisor);
            vect.y = (float)(vect1.y / divisor);
            vect.z = (float)(vect1.z / divisor);
            return vect;
        }

        public static void initialiseVector(ref vec vect)
        {
            vect.x = 0.0f;
            vect.y = 0.0f;
            vect.z = 0.0f;
        }

        public static double calculateAngleInDegreesBetweenTwoVectors(vec pt1, vec pt2)
        {
            double interiorAngle = Math.Acos(dotProduct(pt1, pt2) / (findMagnitudeOfVector(pt1) * findMagnitudeOfVector(pt2)));
            return (interiorAngle / PI) * 180.0;
        }

        public static vec negativeVector(vec vect1)
        {
            vec vect = new vec();
            vect.x = -vect1.x;
            vect.y = -vect1.y;
            vect.z = -vect1.z;
            return vect;
        }

        public static double findMagnitudeOfVector(vec vect1)
        {
            return Math.Sqrt(Math.Pow(vect1.x, 2) + Math.Pow(vect1.y, 2) + Math.Pow(vect1.z, 2));
        }

        public static vec normaliseVector(vec vect1)
        {
            vec normalisedVector = new vec();
            double magnitude = findMagnitudeOfVector(vect1);
            if (magnitude > Constants.DOUBLE_MIN_PRECISION)
            {
                normalisedVector.x = (float)(vect1.x / magnitude);
                normalisedVector.y = (float)(vect1.y / magnitude);
                normalisedVector.z = (float)(vect1.z / magnitude);
            }
            else
            {
                normalisedVector.x = 0.0f;
                normalisedVector.y = 0.0f;
                normalisedVector.z = 0.0f;
            }
            return normalisedVector;
        }

        public static bool checkIfPointLiesOnTriangle3D(poly tri, vec P)
        {
            return sameSide(P, tri.a, tri.b, tri.c) && sameSide(P, tri.b, tri.a, tri.c) && sameSide(P, tri.c, tri.a, tri.b);
        }

        public static bool sameSide(vec p1, vec p2, vec a, vec b)
        {
            vec cp1 = crossProduct(subtractVectors(b, a), subtractVectors(p1, a));
            vec cp2 = crossProduct(subtractVectors(b, a), subtractVectors(p2, a));
            return dotProduct(cp1, cp2) >= 0;
        }

        public static vec calculateIntersectionPointOfLineAndPolygonPlane(vec P0, vec P1, poly pol, ref bool pass)
        {
            vec intersectionP = new vec();
            vec N = calculateNormalOfPoly(pol);
            vec P2 = pol.a;
            vec vecP2minusP0 = subtractVectors(P2, P0);
            vec vecP1minusP0 = subtractVectors(P1, P0);
            if (compareDoubles(dotProduct(N, vecP1minusP0), 0.0))
            {
                pass = false;
                intersectionP.x = Constants.SOME_LARGE_DISTANCE;
                intersectionP.y = Constants.SOME_LARGE_DISTANCE;
                intersectionP.z = Constants.SOME_LARGE_DISTANCE;
            }
            else
            {
                pass = true;
                double t = dotProduct(N, vecP2minusP0) / dotProduct(N, vecP1minusP0);
                intersectionP = addVectors(P0, multiplyVectorByScalar(vecP1minusP0, t));
            }
            return intersectionP;
        }

        public static vec calculateIntersectionOfTwoLinesIn3D(vec LineApoint1, vec LineApoint2, vec LineBpoint1, vec LineBpoint2, ref bool pass, ref bool intersectionOnLineSegments)
        {
            intersectionOnLineSegments = false;
            pass = false;
            vec ip = new vec();

            vec da = subtractVectors(LineApoint2, LineApoint1);
            vec db = subtractVectors(LineBpoint2, LineBpoint1);
            vec dc = subtractVectors(LineBpoint1, LineApoint1);

            if (!compareDoublesArbitraryError(dotProduct(dc, crossProduct(da, db)), 0.0, Constants.DOUBLE_MODERATELY_RELAXED_PRECISION))
            {
                pass = false;
            }
            else
            {
                double s = dotProduct(crossProduct(dc, db), crossProduct(da, db)) / norm2(crossProduct(da, db));

                if (s >= 0.0 && s <= 1.0)
                {
                    intersectionOnLineSegments = true;
                }

                ip = addVectors(LineApoint1, multiplyVectorByScalar(da, s));
                pass = true;
            }

            return ip;
        }

        public static double norm2(vec vect)
        {
            return dotProduct(vect, vect);
        }

        public static vec addThreeVectors(vec A, vec B, vec C)
        {
            return addVectors(addVectors(A, B), C);
        }

        public static vec mapPointFromTriangle1toTriangle2(poly ABC, vec P, poly DEF, ref bool pass)
        {
            vec A = getPolyVertex(ABC, 0);
            vec B = getPolyVertex(ABC, 1);
            vec C = getPolyVertex(ABC, 2);

            vec N = normaliseVector(crossProduct(subtractVectors(B, A), subtractVectors(C, A)));
            float AreaABC = (float)dotProduct(N, crossProduct(subtractVectors(B, A), subtractVectors(C, A)));
            float AreaPBC = (float)dotProduct(N, crossProduct(subtractVectors(B, P), subtractVectors(C, P)));
            float a = AreaPBC / AreaABC;
            float AreaPCA = (float)dotProduct(N, crossProduct(subtractVectors(C, P), subtractVectors(A, P)));
            float b = AreaPCA / AreaABC;
            float c = 1.0f - a - b;

            vec PP = addThreeVectors(multiplyVectorByScalar(A, a), multiplyVectorByScalar(B, b), multiplyVectorByScalar(C, c));
            vec zero = new vec();
            initialiseVector(ref zero);

            if (compareVectors(subtractVectors(PP, P), zero))
            {
                pass = true;
            }
            else
            {
                pass = false;
            }

            vec D = getPolyVertex(DEF, 0);
            vec E = getPolyVertex(DEF, 1);
            vec F = getPolyVertex(DEF, 2);

            vec Pnew = addThreeVectors(multiplyVectorByScalar(D, a), multiplyVectorByScalar(E, b), multiplyVectorByScalar(F, c));
            return Pnew;
        }

        public static double calculateTheDistanceBetweenTwoPoints(vec positionOf1, vec positionOf2)
        {
            return calculateTheDistanceBetweenTwoPoints(positionOf1.x, positionOf2.x, positionOf1.y, positionOf2.y, positionOf1.z, positionOf2.z);
        }

        public static double calculateTheDistanceBetweenTwoPoints(double positionX1, double positionX2, double positionY1, double positionY2, double positionZ1, double positionZ2)
        {
            double xDistanceBetweenTheTwoPoints = positionX1 - positionX2;
            double yDistanceBetweenTheTwoPoints = positionY1 - positionY2;
            double zDistanceBetweenTheTwoPoints = positionZ1 - positionZ2;
            return Math.Sqrt(Math.Pow(xDistanceBetweenTheTwoPoints, 2) + Math.Pow(yDistanceBetweenTheTwoPoints, 2) + Math.Pow(zDistanceBetweenTheTwoPoints, 2));
        }

        public static double calculateTheDistanceBetweenTwoPointsXYOnly(vec positionOf1, vec positionOf2)
        {
            return calculateTheDistanceBetweenTwoPoints(positionOf1.x, positionOf2.x, positionOf1.y, positionOf2.y, 0.0, 0.0);
        }

        public static bool compareDoubles(double a, double b)
        {
            return compareDoublesArbitraryError(a, b, Constants.DOUBLE_MIN_PRECISION);
        }

        public static bool compareDoublesRelaxed(double a, double b)
        {
            return compareDoublesArbitraryError(a, b, 0.01);
        }

        public static bool compareDoublesArbitraryError(double a, double b, double error)
        {
            return (a < (b + error)) && (a > (b - error));
        }
    }
}
