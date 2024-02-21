using UnityEngine;
using HDF.PInvoke;
using System.Runtime.InteropServices;
using System;



public static class Hdf5Reader
{

    public static float[][] GetArray(string fullFilePath, string groupPath, string fieldName)
    {
        float[][] data;
        long fileId = H5F.open(fullFilePath, H5F.ACC_RDONLY);
        try
        {
            var hdf5view = new DataFrameFieldView(fileId, @".\" + groupPath + '/' + fieldName);
            data = hdf5view.GetArray();
        }
        finally
        {
            H5F.close(fileId);
        }
        return data;
    }


    public static float GetField(string fullFilePath, string groupPath, string fieldName)
    {
        return GetArray(fullFilePath, groupPath, fieldName)[0][0];
    }


    public static void PrintDatasets(string fullFilePath)
    { 
        long fileId = H5F.open(fullFilePath, H5F.ACC_RDONLY);

        Debug.Log($"Reading file {fullFilePath}, id: {fileId}");

        H5O.iterate_t iterateCallback = (long loc_id, IntPtr namePtr, ref H5O.info_t info, IntPtr op_data) =>
        {
            byte[] nameBytes = new byte[2048]; // Adjust the size accordingly
            Marshal.Copy(namePtr, nameBytes, 0, nameBytes.Length);
            string objectName = System.Text.Encoding.ASCII.GetString(nameBytes).Split('\0')[0];

            // Check if the object is a dataset
            if (H5O.exists_by_name(loc_id, objectName, H5P.DEFAULT) > 0 && H5O.get_info_by_name(loc_id, objectName, ref info, H5P.DEFAULT) >= 0)
            {
                if (info.type == H5O.type_t.DATASET)
                {
                    long dataSetId = H5D.open(fileId, objectName);
                    long dspace = H5D.get_space(dataSetId);
                    int ndims = H5S.get_simple_extent_ndims(dspace);
                    ulong[] dims = new ulong[ndims];
                    H5S.get_simple_extent_dims(dspace, dims, null);
                    Debug.Log($"Dataset found: {objectName} of shape {string.Join(", ", dims)}");
                    H5D.close(dataSetId);
                }
            }

            return 0;
        };

        // Iterate over objects in the file
        H5O.visit(fileId, H5.index_t.NAME, H5.iter_order_t.INC, iterateCallback, IntPtr.Zero);

        // Close the file
        H5F.close(fileId);
    }
    private struct DataFrameFieldView
    {
        readonly long fileId;
        readonly string datasetPath;
        readonly ulong nRows;
        readonly ulong nCols;


        public DataFrameFieldView(long fileId, string datasetPath)
        {
            long dataSetId = H5D.open(fileId, datasetPath);
            this.fileId = fileId;
            this.datasetPath = datasetPath;
            long dspace = H5D.get_space(dataSetId);
            int ndims = H5S.get_simple_extent_ndims(dspace);

            ulong[] dims = new ulong[ndims];
            H5S.get_simple_extent_dims(dspace, dims, null);

            nRows = dims.Length<1? 1 : dims[0];
            nCols = dims.Length<2? 1: dims[1];

            H5D.close(dataSetId);
        }


        public float[][] GetArray()
        {
            long fieldId = H5D.open(fileId, datasetPath);
            float[,] arr = new float[nRows, nCols];

            long typeId = H5D.get_type(fieldId);
            GCHandle gch = GCHandle.Alloc(arr, GCHandleType.Pinned);
            try
            {
                H5D.read(fieldId, typeId, H5S.ALL, H5S.ALL, H5P.DEFAULT,
                         gch.AddrOfPinnedObject());
            }
            finally
            {
                gch.Free();
            }


            float[][] arrOut = new float[nRows][];
            for (int i = 0; (ulong)i < nRows; i++)
            {
                arrOut[i] = new float[nCols];
                for (int j = 0; (ulong)j < nCols; j++)
                {
                    arrOut[i][j] = arr[i, j];
                }
            }
            H5D.close(fieldId);
            return arrOut;
        }

        public double[] this[int idx]
        {

            get
            {
                long fieldId = H5D.open(fileId, datasetPath);
                double[] arr = new double[nRows];

                // Define the hyperslab to select a single row
                ulong[] start = { (ulong)idx, 0 }; // rowNumber is the row you want to read
                ulong[] count = { 1, nRows }; // numberOfColumns is the width of your dataset

                long rowSpace = H5S.create_simple(2, count, null);
                long dataSpace = H5D.get_space(fieldId);
                H5S.select_hyperslab(dataSpace, H5S.seloper_t.SET, start, null, count, null);

                long typeId = H5D.get_type(fieldId);

                GCHandle gch = GCHandle.Alloc(arr, GCHandleType.Pinned);
                try
                {
                    H5D.read(fieldId, typeId, rowSpace, dataSpace, H5P.DEFAULT,
                                gch.AddrOfPinnedObject());
                }
                finally
                {
                    gch.Free();
                }
                return arr;
            }
        }

    }
}

