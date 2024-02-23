using System;
using System.Collections;
using System.Collections.Generic;
using ModularAgents.Kinematic;
using Mujoco;
using UnityEngine;
using Unity.MLAgents;
using ModularAgents.Kinematic.Mujoco;
using Mujoco.Extensions;
using Unity.MLAgents.Sensors;
using System.Drawing;
using Color = UnityEngine.Color;
using MotionMatch;

public class MjRayGridTextureWriter : MonoBehaviour {

  [SerializeField]
  private int height;

  [SerializeField]
  private int width;

  [SerializeField]
  private float sizeScale;

  [SerializeField]
  private MjBody sourceBody;

  private IKinematic sourceKinematics;

  [SerializeField]
  private RenderTexture renderTexture;

  [SerializeField]
  private Vector3 offset;

  [SerializeField]
  private float bias;

  [SerializeField]
  private float scale;

  [SerializeField, Tooltip("-1 if dynamic height")]
  private float fixedHeight;

  Matrix4x4 UprightMatrix {
    get
    {
      var oldEul = sourceKinematics.Rotation.eulerAngles;
      var newEul = new Vector3(0f, oldEul.y, 0f);

      return Matrix4x4.TRS(sourceKinematics.Position, Quaternion.Euler(newEul), Vector3.one);
    }
  }
  private Vector3 RayCenter => fixedHeight<0 ? UprightMatrix.MultiplyPoint3x4(offset) : UprightMatrix.MultiplyPoint3x4(offset).Horizontal3D()+(offset.y+fixedHeight)*Vector3.up;

  private IEnumerable<Vector3> RayOrigins {
    get
    {
      var mat = UprightMatrix;
      for (int h = 0; h < height; h++) {
        for (int w = 0; w < width; w++) {
          if (fixedHeight < 0) {
            yield return mat.MultiplyPoint3x4(offset + new Vector3(
                (w - (width - 1) / 2f) * sizeScale,
                0f,
                (h - (height - 1) / 2f) * sizeScale));
          } else {

            yield return mat.MultiplyPoint3x4(offset + new Vector3(
                (w - (width - 1) / 2f) * sizeScale,
                0f,
                (h - (height - 1) / 2f) * sizeScale)).Horizontal3D() + (offset.y + fixedHeight) * Vector3.up; ;
          }
        }
      }
    }
  }

  private void WriteToTexture() {
    var mat = UprightMatrix;
    Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
    for (int h = 0; h < height; h++) {
      for (int w = 0; w < width; w++) {
        Vector3 point;
        if (fixedHeight < 0) {
          point = mat.MultiplyPoint3x4(offset + new Vector3(
              (w - (width - 1) / 2f) * sizeScale,
              0f,
              (h - (height - 1) / 2f) * sizeScale));
        } else {

          point = mat.MultiplyPoint3x4(offset + new Vector3(
              (w - (width - 1) / 2f) * sizeScale,
              0f,
              (h - (height - 1) / 2f) * sizeScale)).Horizontal3D() + (offset.y + fixedHeight) * Vector3.up; ;
        }

        var cast = MjState.MjGroundRayCast(new Ray(point, Vector3.down));
        // Set the grayscale values to the texture
        var value = 1 - scale*((float)cast.Item2 - bias);
        Color color = new Color(value, value, value);
        texture.SetPixel(w, h, color);
      }
    }

    texture.Apply();

    // Set the temporary texture to the render texture
    Graphics.Blit(texture, renderTexture);

    // Release the temporary texture
    Destroy(texture);
  }


  private void Start() {
    sourceKinematics = sourceBody.transform.GetIKinematic();
  }

  private void FixedUpdate() {
    WriteToTexture();
  }

  private void OnDrawGizmosSelected() {
    if (!Application.isPlaying) return;
    Gizmos.color = Color.yellow;

    foreach (var origin in RayOrigins ) {
      var pos = origin - bias * Vector3.up;
      Gizmos.DrawWireSphere(pos, 0.05f);
      var cast = MjState.MjGroundRayCast(new Ray(pos, Vector3.down));
      Gizmos.DrawRay(pos, Vector3.down*(float)cast.Item2);
      Gizmos.DrawWireCube(pos + Vector3.down * (float)cast.Item2, 0.05f*Vector3.one);
    }
    
  }
}
