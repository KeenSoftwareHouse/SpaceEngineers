#if !XB1

using System;
using System.Collections.Generic;
using VRageMath;
using System.IO;
#if !XB1
using System.Text.RegularExpressions;
#endif // !XB1
using System.Globalization;
using VRage.Game.Models;

//  Class for loading OBJ files (3d models).

//  Caller of this method must check if exception occur during calling this constructor. Just place it in try/catch.
//  This class will dispose/close its internal objects itself so nothing will be handing around after an exception.

namespace Sandbox.Common
{
    class MyModelObj
    {
        public List<Vector3> Vertexes;
        public List<Vector3> Normals;
        public List<MyTriangleVertexIndices> Triangles;

        public MyModelObj(string filename)
        {
            Vertexes = new List<Vector3>();
            Normals = new List<Vector3>();
            Triangles = new List<MyTriangleVertexIndices>();
            // Loop over each tokenized line of the OBJ file
            foreach (String[] lineTokens in GetLineTokens(filename))
            {
                ParseObjLine(lineTokens);
            }
        }

        /// <summary>
        /// Yields an array of tokens for each line in an OBJ or MTL file.
        /// </summary>
        /// <remarks>
        /// OBJ and MTL files are text based formats of identical structure.
        /// Each line of a OBJ or MTL file is either an instruction or a comment.
        /// Comments begin with # and are effectively ignored.
        /// Instructions are a space dilimited list of tokens. The first token is the
        /// instruction type code. The tokens which follow, are the arguments to that
        /// instruction. The number and format of arguments vary by instruction type.
        /// </remarks>
        /// <param name="filename">Full path of file to read.</param>
        /// <param name="identity">Identity of the file being read. This is modified to
        /// include the current line number in case an exception is thrown.</param>
        /// <returns>Element 0 is the line type identifier. The remaining elements are
        /// arguments to the identifier's operation.</returns>
        private IEnumerable<string[]> GetLineTokens(string filename)
        {
            // Open the file
            using (StreamReader reader = new StreamReader(filename))
            {
                int lineNumber = 1;

                // For each line of the file
                while (!reader.EndOfStream)
                {
#if XB1
                    System.Diagnostics.Debug.Assert(false, "TODO for XB1.");
                    string[] lineTokens = { };//TODO for XB1: Regex.Split(reader.ReadLine().Trim(), @"\s+");
#else // !XB1
                    // Tokenize line by splitting on 1 more more whitespace character
                    string[] lineTokens = Regex.Split(reader.ReadLine().Trim(), @"\s+");
#endif // !XB1

                    // Skip blank lines and comments
                    if (lineTokens.Length > 0 &&
                        lineTokens[0] != String.Empty &&
                        !lineTokens[0].StartsWith("#"))
                    {
                        // Pass off the tokens of this line to be processed
                        yield return lineTokens;
                    }

                    // Done with this line!
                    lineNumber++;
                }
            }
        }

        /// <summary>
        /// Parses and executes an individual line of an OBJ file.
        /// </summary>
        /// <param name="lineTokens">Line to parse as tokens</param>
        private void ParseObjLine(string[] lineTokens)
        {
            // Switch by line type
            switch (lineTokens[0].ToLower())
            {
                // Positions
                case "v":
                    Vertexes.Add(ParseVector3(lineTokens));
                    break;
                // Normals
                case "vn":
                    Normals.Add(ParseVector3(lineTokens));
                    break;
                // Faces
                case "f":
                    // For each triangle vertex
                    int[] indices = new int[3];
                    for (int vertexIndex = 1; vertexIndex <= 3; vertexIndex++)
                    {
                        string[] characters = lineTokens[vertexIndex].Split('/');
                        if (characters.Length > 0)
                            indices[vertexIndex-1] = int.Parse(characters[0], CultureInfo.InvariantCulture);
                    }
                    Triangles.Add(new MyTriangleVertexIndices(indices[0]-1,indices[1]-1,indices[2]-1));
                    break;
                // Unsupported or invalid line types
                default:
                    break;
            }
        }

        /// <summary>
        /// Parses a Vector3 from tokens of an OBJ file line.
        /// </summary>
        /// <param name="lineTokens">X,Y and Z coordinates in lineTokens[1 through 3].
        /// </param>
        private static Vector3 ParseVector3(string[] lineTokens)
        {
            return new Vector3(
                float.Parse(lineTokens[1], CultureInfo.InvariantCulture),
                float.Parse(lineTokens[2], CultureInfo.InvariantCulture),
                float.Parse(lineTokens[3], CultureInfo.InvariantCulture));
        }
    }
}

#endif