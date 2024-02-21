using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Hdf5DatasetPrinter : MonoBehaviour
{
    [SerializeField]
    TextAsset binaryFile;

    private void Awake()
    {
        Debug.Log(AssetDatabase.GetAssetPath(binaryFile));
        Hdf5Reader.PrintDatasets(AssetDatabase.GetAssetPath(binaryFile));
    }
}
