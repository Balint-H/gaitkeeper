using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MotionMatch
{
    public class MMRecipientController : MMController
    {
        [SerializeField]
        MMController donorController;

        protected override MotionMatcher.MMFrame FindFrame(List<Vector2> inputFeatures, MotionMatcher.MMFrame queryFrame)
        {
            float[] donorFeatures = donorController.MotionMatcher.BuildQuery(inputFeatures, donorController.CurFrame);
            float[] recipientFeatures = MotionMatcher.BuildQuery(inputFeatures, CurFrame);
            for (int i = 0; i < 12; i++)
            {
                donorFeatures[i] = recipientFeatures[i];
            }

            return MotionMatcher.Match(donorFeatures);
        }
    }
}
