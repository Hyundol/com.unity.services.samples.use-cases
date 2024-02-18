using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace Unity.Services.Samples.InGameMailbox
{
    public class InGameMailboxSceneManager : MonoBehaviour
    {
        public InGameMailboxSampleView sceneView;
        public static readonly int maxInboxSize = 5;

        public string selectedMessageId { get; private set; }

        async void Start()
        {
            try
            {
                await InitializeUnityServices();

                // Check that scene has not been unloaded while processing async wait to prevent throw.
                if (this == null) return;

                await FetchUpdatedServicesData();
                if (this == null) return;

                sceneView.SetInteractable(true);
                sceneView.RefreshView();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        async Task InitializeUnityServices()
        {
            await UnityServices.InitializeAsync();
            if (this == null) return;

            Debug.Log("Services Initialized.");

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log("Signing in...");
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                if (this == null) return;
            }

            Debug.Log($"Player id: {AuthenticationService.Instance.PlayerId}");

            await EconomyManager.instance.RefreshEconomyConfiguration();
        }

        async Task FetchUpdatedServicesData()
        {
            // This method must execute before AddressablesManager.instance.PreloadAllEconomySprites()
            EconomyManager.instance.InitializeEconomyLookups();

            await Task.WhenAll(
                EconomyManager.instance.RefreshCurrencyBalances(), // 재화 최신화 
                AddressablesManager.instance.PreloadAllEconomySprites() // 재화에 해당하는 아이콘 사전 로드 (게임 진행에 유용)
            );
            if (this == null) return;

            await FetchUpdatedInboxData();
        }

        async Task FetchUpdatedInboxData()
        {
            await Task.WhenAll(
                RemoteConfigManager.instance.FetchConfigs(),
                CloudSaveManager.instance.FetchPlayerInbox()
            );
            if (this == null) return;

            CloudSaveManager.instance.DeleteExpiredMessages(); // 만료된 메세지 삭제
            CloudSaveManager.instance.CheckForNewMessages(); // 새로운 메세지 확인
            await CloudSaveManager.instance.SavePlayerInboxInCloudSave(); // 플레이어의 받은 편지를 클라우드에 저장.
        }

        async void Update()
        {
            try
            {
                // Note a more optimized implementation would only ask CloudSaveManager to delete expired messages
                // once a minute.
                if (CloudSaveManager.instance.DeleteExpiredMessages() > 0)
                {
                    CloudSaveManager.instance.CheckForNewMessages();
                    await CloudSaveManager.instance.SavePlayerInboxInCloudSave();
                    if (this == null) return;

                    sceneView.RefreshView();
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public async void SelectMessage(string messageId)
        {
            try
            {
                selectedMessageId = messageId;
                CloudSaveManager.instance.MarkMessageAsRead(messageId);
                await CloudSaveManager.instance.SavePlayerInboxInCloudSave();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public async void OnDeleteOpenMessageButtonPressed()
        {
            try
            {
                CloudSaveManager.instance.DeleteMessage(selectedMessageId);
                sceneView.RefreshView();
                await CloudSaveManager.instance.SavePlayerInboxInCloudSave();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public async void OnClaimOpenMessageAttachmentButtonPressed()
        {
            try
            {
                await CloudCodeManager.instance.CallClaimMessageAttachmentEndpoint(selectedMessageId);
                if (this == null) return;

                await UpdateSceneAfterClaim();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        async Task UpdateSceneAfterClaim()
        {
            await Task.WhenAll(
                EconomyManager.instance.RefreshCurrencyBalances(),
                CloudSaveManager.instance.FetchPlayerInbox()
            );
            if (this == null) return;

            sceneView.RefreshView();
        }

        public async void OnClaimAllButtonPressed()
        {
            try
            {
                await CloudCodeManager.instance.CallClaimAllMessageAttachmentsEndpoint();
                if (this == null) return;

                await UpdateSceneAfterClaim();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public async void OnDeleteAllReadAndClaimedButtonPressed()
        {
            try
            {
                if (CloudSaveManager.instance.DeleteAllReadAndClaimedMessages() > 0)
                {
                    sceneView.RefreshView();
                    await CloudSaveManager.instance.SavePlayerInboxInCloudSave();
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public async void OnOpenInventoryButtonPressed()
        {
            try
            {
                await sceneView.ShowInventoryPopup();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public async void OnResetInbox()
        {
            try
            {
                sceneView.SetInteractable(false);
                selectedMessageId = "";
                var desiredAudience = (RemoteConfigManager.SampleAudience)sceneView.audienceDropdown.value;
                RemoteConfigManager.instance.UpdateAudienceType(desiredAudience);
                await CloudSaveManager.instance.ResetCloudSaveData();
                if (this == null) return;

                await FetchUpdatedInboxData();
                if (this == null) return;

                sceneView.SetInteractable(true);
                sceneView.RefreshView();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
