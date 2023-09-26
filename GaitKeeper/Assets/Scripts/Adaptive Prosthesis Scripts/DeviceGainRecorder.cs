using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mujoco;
using System.Linq;

namespace ModularAgents.MotorControl
{

    public class DeviceGainRecorder : MonoBehaviour
    {
        // Start is called before the first frame update
        [SerializeField]
        Transform foot;

        [SerializeField]
        AdaptiveProsthesisActuator deviceActuator;

        List<List<float>> lines;

        void Start()
        {
            lines = new List<List<float>>();
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            lines.Add(new List<float> { (float)deviceActuator.PosGain, (float)deviceActuator.VelGain, foot.transform.position.y });
        }

        private void OnApplicationQuit()
        {
            var header = "PosGain, VelGain, FootHeight";

            System.IO.File.WriteAllLines(Application.dataPath + "/SavedLists.csv", lines.Select(l => string.Join(", ", l)).Prepend(header));
        }
    }
}