using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameOperationsSamples
{
    public class RewardPopupView : MonoBehaviour
    {
        public TextMeshProUGUI headerText;

        public RewardDisplayView rewardDisplayView;

        public Button closeButton;

        public TextMeshProUGUI closeButtonText;


        public void Show(List<RewardDetail> rewards)
        {
            rewardDisplayView.PopulateView(rewards);
            gameObject.SetActive(true);
        }

        public void Close()
        {
            var currentGameObject = gameObject;
            currentGameObject.SetActive(false);
            Destroy(currentGameObject);
        }
    }
}