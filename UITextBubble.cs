using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using MergeMarines.UI;

namespace MergeMarines
{
    public class UITextBubble : SingletonGameObject<UITextBubble>
    {
        private const int TextOffset = 80;
        private const int BubbleLeftOffset = 50;
        private const float AnimationDuration = 0.25f;
        private const float AlphaShadow = 0.5f;
        private const float DelayToCloseMessageOverlay = 1.2f;

        [SerializeField]
        private Image _shadowImage = default;
        [SerializeField] 
        private RectTransform _arrow = null;
        [SerializeField]
        private Button _closeButton = null;
        [SerializeField]
        private RectTransform _basicBubble = null;

        [Header("Text Bubble")]
        [SerializeField]
        private RectTransform _textBubble = null;
        [SerializeField]
        private Transform _parent = null;
        [SerializeField]
        private TextMeshProUGUI _bubbleLabel = null;

        [Header("Reward Bubble")]
        [SerializeField]
        private RectTransform _rewardBubblePoint = null;
        [SerializeField]
        private UIBubbleRewardGroup[] _rewardGroups = default;

        [Header("Message Overlay")]
        [SerializeField]
        private TextMeshProUGUI _messageText = null;
        [SerializeField]
        private RectTransform _messageGroup = null;

        private int _message;
        private RectTransform _currentRect = default;

        private void Start()
        {
            _closeButton.onClick.AddListener(OnCloseButtonClick);
        }

        protected override void Init()
        {
            base.Init();

            gameObject.SetActive(false);
            _textBubble.gameObject.SetActive(false);
            _rewardBubblePoint.gameObject.SetActive(false);
            _messageGroup.gameObject.SetActive(false);
            _shadowImage.enabled = false;
        }

        public void Show(TextBubbleType type, Vector3 position, string message, int textSize = 35, Transform forcedParent = null, 
            Vector3 offset = default, bool fromViewPoint = false, bool needShadow = false)
        {
            Show(_textBubble, needShadow: needShadow);
            SetText(message, textSize);
            
            switch (type)
            {
                case TextBubbleType.Up:
                    ChangeArrowYPosition(-_textBubble.rect.height / 2, 0);
                    ChangeBubblePosition(position);
                    break;
                
                case TextBubbleType.Down:
                    ChangeArrowYPosition(_textBubble.rect.height / 2, 180);
                    ChangeBubblePosition(position);
                    break;

                default:
#if UNITY_EDITOR
                    Debug.LogException(new Exception($"Not found bubble settings for {nameof(TextBubbleType)}: {type}"));
#endif
                    break;
            }

            if (forcedParent)
            {
                _textBubble.SetParent(forcedParent);
            }

            if (fromViewPoint)
            {
                position = UISystem.Instance.Camera.ViewportToScreenPoint(position);
                position.z = 0;
            }

            _textBubble.position = position + offset;
        }

        private void ChangeBubblePosition(Vector3 position)
        {
            var sizeDeltaXRight = position.x * Screen.width + _textBubble.sizeDelta.x / 2;
            var sizeDeltaXLeft = position.x * Screen.width - _textBubble.sizeDelta.x / 2;
            
            if (sizeDeltaXRight > Screen.width)
            {
                var bubbleOverlay = sizeDeltaXRight - Screen.width;
                _basicBubble.anchoredPosition = new Vector2(- bubbleOverlay - BubbleLeftOffset, _basicBubble.anchoredPosition.y);
                _textBubble.anchoredPosition = new Vector2(0, 0);  
            }
            else if (sizeDeltaXLeft < 0)
            {
                _basicBubble.anchoredPosition = new Vector2(-sizeDeltaXLeft, _basicBubble.anchoredPosition.y);
                _textBubble.anchoredPosition = new Vector2(0, 0);  
            }
            else
            {
                _basicBubble.anchoredPosition = new Vector2(0, _basicBubble.anchoredPosition.y);
            }
        }

        private void ChangeArrowYPosition(float yPosition, float angel)
        {
            var arrowLocalPosition = _arrow.transform.localPosition;
            
            _arrow.transform.localPosition = new Vector3(arrowLocalPosition.x, yPosition, arrowLocalPosition.z);
            _arrow.transform.rotation = Quaternion.Euler(0,0,angel);
        }

        public void ShowCurrencyBubble(WaveStageData stage, Vector2 position, Vector2 offset = default, bool transformToWorld = false, bool needShadow = false)
        {
            Show(_rewardBubblePoint, needShadow: needShadow);

            if (transformToWorld) 
                position = UISystem.Instance.Camera.WorldToScreenPoint(position);

            _rewardBubblePoint.position = position + offset;

            int minCoins = stage.GetAllCoinsDrop();
            int maxCoins = stage.GetAllCoinsDrop();

            _rewardGroups[0].Refresh(ItemType.Coin, minCoins, maxCoins);

            for (int i = 2; i < _rewardGroups.Length; i++)
            {
                ItemDropData drop = stage.StageDrop[i - 2];
                _rewardGroups[i].Refresh(drop.RewardItem.Type, Mathf.Approximately(drop.Chance, 1f) ? drop.RewardItem.Count : 0, drop.RewardItem.Count);
            }
        }

        public void ShowMessageOverlay(string message, bool needShadow = false)
        {
            Show(_messageGroup, needShadow: needShadow);
            _messageText.text = message;

            this.InvokeWithDelay(DelayToCloseMessageOverlay, () =>
            {
                if (_currentRect.localScale.y.AlmostEquals(1f))
                    OnCloseButtonClick();
            });
        }

        private void Show(RectTransform targetRect, Vector3 targetScale = default, bool needShadow = false)
        {
            gameObject.SetActive(true);
            targetRect.gameObject.SetActive(true);
            _currentRect = targetRect;

            _shadowImage.enabled = needShadow;
            if (needShadow)
            {
                _shadowImage.DOFade(AlphaShadow, AnimationDuration);
            }
            
            targetRect.localScale = Vector3.zero;
            targetRect.DOScale(targetScale == default ? Vector3.one : targetScale, AnimationDuration);
        }

        private void SetText(string message, int textSize)
        {
            _bubbleLabel.text = message;
            _bubbleLabel.fontSize = textSize;
            _bubbleLabel.ForceMeshUpdate();
            Vector2 text = _bubbleLabel.GetRenderedValues();
            _textBubble.sizeDelta = _textBubble.sizeDelta.ChangeX(text.x + TextOffset);
            _textBubble.sizeDelta = _textBubble.sizeDelta.ChangeY(text.y + TextOffset);
        }

        public void Hide()
        {
            if (_currentRect == default)
                return;

            if (_shadowImage.enabled)
            {
                var doFade = _shadowImage.DOFade(0f, AnimationDuration);
                doFade.onComplete += () => _shadowImage.enabled = false;
            }

            var tweenerCore = _currentRect.DOScale(Vector3.zero, AnimationDuration);
            tweenerCore.onComplete += () =>
            {
                gameObject.SetActive(false);
                _currentRect.SetParent(_parent);
                _currentRect.anchoredPosition = Vector2.zero;
                _currentRect = default;
            };
        }
        
        public static void ForceHide()
        {
            Instance.gameObject.SetActive(false);
        }

        private void OnCloseButtonClick()
        {
            Hide();
        }
    }
}