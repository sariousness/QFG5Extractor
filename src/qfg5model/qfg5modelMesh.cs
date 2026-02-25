using System;
using System.IO;
using System.Text;

namespace QFG5Extractor.qfg5model
{
    public class qfg5modelMesh
    {
        public static void importOrExportMeshFile(ref byte[] mdlMeshData, out byte[] mdlMeshDataNew, int numberOfSubmeshes, int mdlMeshDataAddress, string inputFileNameBase, string outputFileNameBase, bool importMesh)
        {
            mdlMeshDataNew = null;
            byte[][] newsubmeshArray = new byte[numberOfSubmeshes][];

            int[] submeshAddressArray = new int[numberOfSubmeshes];
            string[] submeshNameArray = new string[numberOfSubmeshes];

            int submeshAddressReferenceAddress = 0;
            for (int submeshIndex = 0; submeshIndex < numberOfSubmeshes; submeshIndex++)
            {
                if (submeshAddressReferenceAddress + 4 > mdlMeshData.Length) break;
                byte[] submeshAddressTempByteArray = new byte[] { mdlMeshData[submeshAddressReferenceAddress], mdlMeshData[submeshAddressReferenceAddress + 1], mdlMeshData[submeshAddressReferenceAddress + 2], mdlMeshData[submeshAddressReferenceAddress + 3] };
                int submeshAddressTemp = (int)BitConverter.ToUInt32(submeshAddressTempByteArray, 0);
                submeshAddressArray[submeshIndex] = submeshAddressTemp;

                submeshAddressReferenceAddress += Constants.BYTES_PER_INT;
            }

            for (int submeshIndex = 0; submeshIndex < numberOfSubmeshes; submeshIndex++)
            {
                int submeshAddressRelative = submeshAddressArray[submeshIndex] - mdlMeshDataAddress;
                if (submeshAddressRelative < 0 || submeshAddressRelative + Constants.SUBMESH_NAME_LENGTH > mdlMeshData.Length) continue;

                string submeshName = "";
                for (int i = 0; i < Constants.SUBMESH_NAME_LENGTH; i++)
                {
                    submeshName += (char)mdlMeshData[submeshAddressRelative + i];
                }

                int lastIndexOfRealCharacterInName = 0;
                bool finishedReadingRealCharactersInName = false;
                for (int i = 0; i < submeshName.Length; i++)
                {
                    if (!finishedReadingRealCharactersInName)
                    {
                        if ((int)(submeshName[i]) > 48 && (int)(submeshName[i]) < 122)
                        {
                            lastIndexOfRealCharacterInName++;
                        }
                        else
                        {
                            finishedReadingRealCharactersInName = true;
                        }
                    }
                }
                submeshName = submeshName.Substring(0, lastIndexOfRealCharacterInName);
                submeshNameArray[submeshIndex] = submeshName;

                StreamWriter fileObjectLDR = null;
                StreamWriter fileObjectHakenberg = null;
                
                string submeshIndexString = qfg5modelShared.convertIntToManagedString(submeshIndex);
                string suffix = "-" + submeshIndexString + "-" + submeshName;
                string fileNameLDR = outputFileNameBase + suffix + Constants.LDR_EXTENSION;
                
                string fileNameHakenberg = "";
                if (importMesh)
                {
                    fileNameHakenberg = inputFileNameBase + suffix + Constants.HAK_EXTENSION;
                    if (!File.Exists(fileNameHakenberg))
                    {
                        Console.WriteLine("Warning: Expected HAK file not found: " + fileNameHakenberg);
                        continue;
                    }
                }
                else
                {
                    fileNameHakenberg = outputFileNameBase + suffix + Constants.HAK_EXTENSION;
                    fileObjectLDR = new StreamWriter(File.Open(fileNameLDR, FileMode.Create));
                    fileObjectHakenberg = new StreamWriter(File.Open(fileNameHakenberg, FileMode.Create));
                }

                byte[] tempByteArray1 = new byte[] { mdlMeshData[submeshAddressRelative + 0x60], mdlMeshData[submeshAddressRelative + 0x61], mdlMeshData[submeshAddressRelative + 0x62], mdlMeshData[submeshAddressRelative + 0x63] };
                int numberOfVertices = (int)BitConverter.ToUInt32(tempByteArray1, 0);
                byte[] tempByteArray2 = new byte[] { mdlMeshData[submeshAddressRelative + 0x64], mdlMeshData[submeshAddressRelative + 0x65], mdlMeshData[submeshAddressRelative + 0x66], mdlMeshData[submeshAddressRelative + 0x67] };
                int numberOfUVcoords = (int)BitConverter.ToUInt32(tempByteArray2, 0);
                byte[] tempByteArray3 = new byte[] { mdlMeshData[submeshAddressRelative + 0x68], mdlMeshData[submeshAddressRelative + 0x69], mdlMeshData[submeshAddressRelative + 0x6A], mdlMeshData[submeshAddressRelative + 0x6B] };
                int numberOfFaces = (int)BitConverter.ToUInt32(tempByteArray3, 0);
                int numberOfLightingEntries = numberOfVertices;

                if (!importMesh)
                {
                    fileObjectHakenberg.WriteLine("% vertices");
                    fileObjectHakenberg.WriteLine("v 3 " + qfg5modelShared.convertIntToManagedString(numberOfVertices));
                }

                string fileHAKLine = "";

                int submeshVerticesArraySize = numberOfVertices * Constants.SUBMESH_DATAPOINTS_PER_VERTEX;
                int submeshUVcoordsArraySize = numberOfUVcoords * Constants.SUBMESH_DATAPOINTS_PER_UV_COORDS;
                int submeshFaceArraySize = numberOfFaces * 10;
                int submeshLightingEntriesArraySize = numberOfLightingEntries * Constants.SUBMESH_DATAPOINTS_PER_LIGHTING_ENTRY;

                // Safety checks for huge allocations or garbage bytes
                if (submeshVerticesArraySize < 0 || submeshUVcoordsArraySize < 0 || submeshFaceArraySize < 0 || submeshLightingEntriesArraySize < 0) {
                    continue; 
                }

                if (submeshVerticesArraySize > 1000000 || submeshUVcoordsArraySize > 1000000 || submeshFaceArraySize > 1000000 || submeshLightingEntriesArraySize > 1000000) {
                    Logger.LogWarning($"Skipping submesh '{submeshName}' in {inputFileNameBase} due to out-of-bounds array geometry allocation requirements (potentially corrupted or unmapped MDL variation).");
                    continue;
                }

                float[] submeshVerticesArray;
                float[] submeshUVcoordsArray;
                float[] submeshFaceArray;
                int[] submeshLightingEntriesArray;

                int submeshAddressRelativeVertices = submeshAddressRelative + Constants.SUBMESH_HEADER_VERTICES_LIST_ADDRESS;
                int submeshAddressRelativeUVcoords = submeshAddressRelativeVertices + submeshVerticesArraySize * Constants.BYTES_PER_INT;
                int submeshAddressRelativeFaces = submeshAddressRelativeUVcoords + submeshUVcoordsArraySize * Constants.BYTES_PER_INT;
                int submeshAddressRelativeLightingEntries = submeshAddressRelativeFaces + submeshFaceArraySize * Constants.BYTES_PER_INT;

                if (submeshAddressRelativeLightingEntries + submeshLightingEntriesArraySize * Constants.BYTES_PER_INT > mdlMeshData.Length) continue;

                try
                {
                    submeshVerticesArray = new float[submeshVerticesArraySize];
                    submeshUVcoordsArray = new float[submeshUVcoordsArraySize];
                    submeshFaceArray = new float[numberOfFaces * Constants.SUBMESH_DATAPOINTS_PER_FACE];
                    submeshLightingEntriesArray = new int[numberOfLightingEntries * Constants.SUBMESH_DATAPOINTS_PER_LIGHTING_ENTRY];

                    for (int i = 0; i < submeshVerticesArraySize; i++)
                    {
                        int currentAddress = submeshAddressRelativeVertices + (i * Constants.BYTES_PER_INT);
                        byte[] tempByteArray = new byte[] { mdlMeshData[currentAddress + 0], mdlMeshData[currentAddress + 1], mdlMeshData[currentAddress + 2], mdlMeshData[currentAddress + 3] };
                        submeshVerticesArray[i] = qfg5modelShared.bytesToFloat(tempByteArray);

                        if (!importMesh)
                        {
                            fileHAKLine += qfg5modelShared.convertFloatToManagedString(submeshVerticesArray[i]) + " ";
                            if (i % Constants.SUBMESH_DATAPOINTS_PER_VERTEX == Constants.SUBMESH_DATAPOINTS_PER_VERTEX - 1)
                            {
                                fileObjectHakenberg.WriteLine(fileHAKLine);
                                fileHAKLine = "";
                            }
                        }
                    }

                    for (int i = 0; i < submeshUVcoordsArraySize; i++)
                    {
                        int currentAddress = submeshAddressRelativeUVcoords + (i * Constants.BYTES_PER_INT);
                        byte[] tempByteArray = new byte[] { mdlMeshData[currentAddress + 0], mdlMeshData[currentAddress + 1], mdlMeshData[currentAddress + 2], mdlMeshData[currentAddress + 3] };
                        submeshUVcoordsArray[i] = qfg5modelShared.bytesToFloat(tempByteArray);
                    }
                }
                catch (Exception e)
                {
                    Logger.LogWarning($"Exception mapping arrays for submesh '{submeshName}' in {inputFileNameBase}: {e.Message}");
                    continue; // Abort processing this specific submesh
                }

                if (!importMesh)
                {
                    fileObjectHakenberg.WriteLine("% triangles");
                    fileObjectHakenberg.WriteLine("f 2 " + qfg5modelShared.convertIntToManagedString(submeshFaceArraySize / Constants.SUBMESH_DATAPOINTS_PER_FACE));
                }
                
                fileHAKLine = "";

                for (int i = 0; i < submeshFaceArraySize; i++)
                {
                    int currentAddress = submeshAddressRelativeFaces + (i * Constants.BYTES_PER_INT);
                    byte[] tempByteArray = new byte[] { mdlMeshData[currentAddress + 0], mdlMeshData[currentAddress + 1], mdlMeshData[currentAddress + 2], mdlMeshData[currentAddress + 3] };

                    int faceInternalDataPointIndex = i % Constants.SUBMESH_DATAPOINTS_PER_FACE;

                    if (faceInternalDataPointIndex == 0)
                    {
                        fileHAKLine = "";
                    }
                    
                    if (faceInternalDataPointIndex < Constants.SUBMESH_DATAPOINTS_FACE_INDEX_OF_FIRST_UVCOORD)
                    {
                        int vertexIndex = (int)BitConverter.ToUInt32(tempByteArray, 0);
                        submeshFaceArray[i] = (float)vertexIndex;
                        
                        if (!importMesh)
                        {
                            fileHAKLine += qfg5modelShared.convertIntToManagedString(vertexIndex) + " ";
                        }
                    }
                    else if (faceInternalDataPointIndex < Constants.SUBMESH_DATAPOINTS_FACE_INDEX_OF_UNKNOWN)
                    {
                        int UVcoordsIndex = (int)BitConverter.ToUInt32(tempByteArray, 0);
                        submeshFaceArray[i] = (float)UVcoordsIndex;
                    }
                    else if (faceInternalDataPointIndex == Constants.SUBMESH_DATAPOINTS_FACE_INDEX_OF_UNKNOWN)
                    {
                        int unknownInt = (int)BitConverter.ToUInt32(tempByteArray, 0);
                        submeshFaceArray[i] = (float)unknownInt;
                    }
                    else
                    {
                        submeshFaceArray[i] = qfg5modelShared.bytesToFloat(tempByteArray);
                    }

                    if (!importMesh)
                    {
                        if (faceInternalDataPointIndex == Constants.SUBMESH_DATAPOINTS_PER_FACE - 1)
                        {
                            fileObjectHakenberg.WriteLine(fileHAKLine);
                        }
                    }
                }

                for (int i = 0; i < submeshLightingEntriesArraySize; i++)
                {
                    int currentAddress = submeshAddressRelativeLightingEntries + (i * Constants.BYTES_PER_INT);
                    byte[] tempByteArray = new byte[] { mdlMeshData[currentAddress + 0], mdlMeshData[currentAddress + 1], mdlMeshData[currentAddress + 2], mdlMeshData[currentAddress + 3] };
                    submeshLightingEntriesArray[i] = (int)BitConverter.ToUInt32(tempByteArray, 0);
                }

                if (!importMesh)
                {
                    fileObjectLDR?.Close();
                    fileObjectHakenberg?.Close();
                }

                if (importMesh)
                {
                    float[] newsubmeshVerticesArray = null;
                    float[] newsubmeshUVcoordsArray = null;
                    float[] newsubmeshFaceArray = null;
                    int[] newsubmeshLightingEntriesArray = null;

                    qfg5modelMeshImport.importHAKfile(fileNameHakenberg, out newsubmeshVerticesArray, out newsubmeshFaceArray);
                    qfg5modelMeshImport.formatNew3DmeshData(submeshName, submeshVerticesArray, submeshUVcoordsArray, submeshFaceArray, submeshLightingEntriesArray, newsubmeshVerticesArray, out newsubmeshUVcoordsArray, newsubmeshFaceArray, out newsubmeshLightingEntriesArray);

                    int newsubmeshVerticesArraySize = newsubmeshVerticesArray.Length;
                    int newsubmeshUVcoordsArraySize = newsubmeshUVcoordsArray.Length;
                    int newsubmeshFaceArraySize = newsubmeshFaceArray.Length;
                    int newsubmeshLightingEntriesArraySize = newsubmeshLightingEntriesArray.Length;
                    
                    int newnumberOfVertices = newsubmeshVerticesArraySize / Constants.SUBMESH_DATAPOINTS_PER_VERTEX;
                    int newnumberOfUVcoords = newsubmeshUVcoordsArraySize / Constants.SUBMESH_DATAPOINTS_PER_UV_COORDS;
                    int newnumberOfFaces = newsubmeshFaceArraySize / Constants.SUBMESH_DATAPOINTS_PER_FACE;
                    int newnumberOfLightingEntries = newsubmeshLightingEntriesArraySize / Constants.SUBMESH_DATAPOINTS_PER_LIGHTING_ENTRY;

                    long newsubmeshSizeLong = (long)Constants.SUBMESH_HEADER_VERTICES_LIST_ADDRESS + (long)newsubmeshVerticesArraySize * Constants.BYTES_PER_INT + (long)newsubmeshUVcoordsArraySize * Constants.BYTES_PER_INT + (long)newsubmeshFaceArraySize * Constants.BYTES_PER_INT + (long)newsubmeshLightingEntriesArraySize * Constants.BYTES_PER_INT;
                    if (newsubmeshSizeLong > int.MaxValue) throw new OverflowException("Submesh size too large");
                    int newsubmeshSize = (int)newsubmeshSizeLong;
                    
                    byte[] newsubmesh = new byte[newsubmeshSize];
                    newsubmeshArray[submeshIndex] = newsubmesh;

                    int newsubmeshCurrentIndex = 0;

                    for (int i = 0; i < Constants.SUBMESH_HEADER_NUMBER_OF_VERTICES_ADDRESS; i++)
                    {
                        newsubmesh[i] = mdlMeshData[submeshAddressRelative + i];
                        newsubmeshCurrentIndex++;
                    }

                    insertByteArray(newsubmesh, BitConverter.GetBytes(newnumberOfVertices), newsubmeshCurrentIndex);
                    newsubmeshCurrentIndex += Constants.BYTES_PER_INT;
                    
                    insertByteArray(newsubmesh, BitConverter.GetBytes(newnumberOfUVcoords), newsubmeshCurrentIndex);
                    newsubmeshCurrentIndex += Constants.BYTES_PER_INT;
                    
                    insertByteArray(newsubmesh, BitConverter.GetBytes(newnumberOfFaces), newsubmeshCurrentIndex);
                    newsubmeshCurrentIndex += Constants.BYTES_PER_INT;

                    int register7C = Constants.SUBMESH_HEADER_VERTICES_LIST_ADDRESS;
                    insertByteArray(newsubmesh, BitConverter.GetBytes(register7C), newsubmeshCurrentIndex);
                    newsubmeshCurrentIndex += Constants.BYTES_PER_INT;
                    
                    int registerR1 = Constants.SUBMESH_HEADER_VERTICES_LIST_ADDRESS + newsubmeshVerticesArraySize * Constants.BYTES_PER_INT;
                    insertByteArray(newsubmesh, BitConverter.GetBytes(registerR1), newsubmeshCurrentIndex);
                    newsubmeshCurrentIndex += Constants.BYTES_PER_INT;

                    int registerR2 = Constants.SUBMESH_HEADER_VERTICES_LIST_ADDRESS + newsubmeshVerticesArraySize * Constants.BYTES_PER_INT + newsubmeshUVcoordsArraySize * Constants.BYTES_PER_INT;
                    insertByteArray(newsubmesh, BitConverter.GetBytes(registerR2), newsubmeshCurrentIndex);
                    newsubmeshCurrentIndex += Constants.BYTES_PER_INT;
                    
                    int registerR3 = Constants.SUBMESH_HEADER_VERTICES_LIST_ADDRESS + newsubmeshVerticesArraySize * Constants.BYTES_PER_INT + newsubmeshUVcoordsArraySize * Constants.BYTES_PER_INT + newsubmeshFaceArraySize * Constants.BYTES_PER_INT;
                    insertByteArray(newsubmesh, BitConverter.GetBytes(registerR3), newsubmeshCurrentIndex);
                    newsubmeshCurrentIndex += Constants.BYTES_PER_INT;

                    for (int i = 0; i < newsubmeshVerticesArraySize; i++)
                    {
                        insertByteArray(newsubmesh, BitConverter.GetBytes(newsubmeshVerticesArray[i]), newsubmeshCurrentIndex);
                        newsubmeshCurrentIndex += Constants.BYTES_PER_INT;
                    }
                    for (int i = 0; i < newsubmeshUVcoordsArraySize; i++)
                    {
                        insertByteArray(newsubmesh, BitConverter.GetBytes(newsubmeshUVcoordsArray[i]), newsubmeshCurrentIndex);
                        newsubmeshCurrentIndex += Constants.BYTES_PER_INT;
                    }
                    for (int i = 0; i < newsubmeshFaceArraySize; i++)
                    {
                        byte[] tempByteArray;
                        int faceInternalDataPointIndex = i % Constants.SUBMESH_DATAPOINTS_PER_FACE;
                        
                        if (faceInternalDataPointIndex < Constants.SUBMESH_DATAPOINTS_FACE_INDEX_OF_FIRST_UVCOORD)
                        {
                            tempByteArray = BitConverter.GetBytes((int)newsubmeshFaceArray[i]);
                        }
                        else if (faceInternalDataPointIndex < Constants.SUBMESH_DATAPOINTS_FACE_INDEX_OF_UNKNOWN)
                        {
                            tempByteArray = BitConverter.GetBytes((int)newsubmeshFaceArray[i]);
                        }
                        else if (faceInternalDataPointIndex == Constants.SUBMESH_DATAPOINTS_FACE_INDEX_OF_UNKNOWN)
                        {
                            tempByteArray = BitConverter.GetBytes((int)newsubmeshFaceArray[i]);
                        }
                        else
                        {
                            tempByteArray = BitConverter.GetBytes(newsubmeshFaceArray[i]);
                        }
                        
                        insertByteArray(newsubmesh, tempByteArray, newsubmeshCurrentIndex);
                        newsubmeshCurrentIndex += Constants.BYTES_PER_INT;
                    }
                    for (int i = 0; i < newsubmeshLightingEntriesArraySize; i++)
                    {
                        insertByteArray(newsubmesh, BitConverter.GetBytes(newsubmeshLightingEntriesArray[i]), newsubmeshCurrentIndex);
                        newsubmeshCurrentIndex += Constants.BYTES_PER_INT;
                    }
                }
            }

            if (importMesh)
            {
                long totalSizeOfSubmeshArrays = 0;
                for (int submeshIndex = 0; submeshIndex < numberOfSubmeshes; submeshIndex++)
                {
                    if (newsubmeshArray[submeshIndex] != null)
                    {
                        totalSizeOfSubmeshArrays += (long)newsubmeshArray[submeshIndex].Length;
                    }
                }

                int mdlMeshDataHeaderSize = numberOfSubmeshes * Constants.BYTES_PER_INT;
                long mdlMeshDataNewSizeLong = (long)mdlMeshDataHeaderSize + totalSizeOfSubmeshArrays;

                if (mdlMeshDataNewSizeLong > int.MaxValue)
                {
                    throw new OverflowException("Total mesh data size exceeds maximum allowed size.");
                }

                int mdlMeshDataNewSize = (int)mdlMeshDataNewSizeLong;
                mdlMeshDataNew = new byte[mdlMeshDataNewSize];
                byte[] mdlMeshDataNewHeader = new byte[mdlMeshDataHeaderSize];

                int newsubmeshAddressReferenceAddress = 0;
                int mdlMeshDataSubmeshAddressCurrent = mdlMeshDataAddress + mdlMeshDataHeaderSize;
                
                for (int submeshIndex = 0; submeshIndex < numberOfSubmeshes; submeshIndex++)
                {
                    byte[] currentnewsubmesh = newsubmeshArray[submeshIndex];
                    if (currentnewsubmesh == null) continue;

                    byte[] currentsubmeshAddressByteArray = BitConverter.GetBytes(mdlMeshDataSubmeshAddressCurrent);
                    
                    insertByteArray(mdlMeshDataNewHeader, currentsubmeshAddressByteArray, newsubmeshAddressReferenceAddress);
                    insertByteArray(mdlMeshDataNew, currentsubmeshAddressByteArray, newsubmeshAddressReferenceAddress);
                    
                    mdlMeshDataSubmeshAddressCurrent += currentnewsubmesh.Length;
                    newsubmeshAddressReferenceAddress += Constants.BYTES_PER_INT;
                }

                int mdlMeshDataNewIndex = numberOfSubmeshes * Constants.BYTES_PER_INT;
                for (int submeshIndex = 0; submeshIndex < numberOfSubmeshes; submeshIndex++)
                {
                    byte[] currentnewsubmesh = newsubmeshArray[submeshIndex];
                    if (currentnewsubmesh == null) continue;

                    for (int i = 0; i < currentnewsubmesh.Length; i++)
                    {
                        mdlMeshDataNew[mdlMeshDataNewIndex] = currentnewsubmesh[i];
                        mdlMeshDataNewIndex++;
                    }
                }
            }
        }

        private static void insertByteArray(byte[] byteArrayToUpdate, byte[] byteArrayToInsert, int index)
        {
            for (int i = 0; i < byteArrayToInsert.Length; i++)
            {
                byteArrayToUpdate[i + index] = byteArrayToInsert[i];
            }
        }
    }
}
