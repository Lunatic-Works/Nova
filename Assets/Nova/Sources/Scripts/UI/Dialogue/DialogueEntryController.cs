using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nova
{
    public class DialogueEntryLayoutSetting
    {
        internal int leftPadding;
        internal int rightPadding;
        internal float nameTextSpacing;
        internal float preferredHeight = -1f;

        internal static readonly DialogueEntryLayoutSetting Default = new DialogueEntryLayoutSetting();
    }

    public class DialogueEntryController : MonoBehaviour
    {
        private Text nameBox;
        private TMP_Text contentBox;
        public TextProxy contentProxy { get; private set; }

        public DialogueDisplayData displayData { get; private set; }

        private VerticalLayoutGroup verticalLayoutGroup;
        private LayoutElement textLayoutElement;

        private bool inited;

        private void InitReferences()
        {
            if (inited) return;

            nameBox = transform.Find("Name").GetComponent<Text>();
            var textTransform = transform.Find("Content/Text");
            contentBox = textTransform.GetComponent<TMP_Text>();
            contentProxy = textTransform.GetComponent<TextProxy>();
            contentProxy.Init();

            verticalLayoutGroup = GetComponent<VerticalLayoutGroup>();
            textLayoutElement = textTransform.GetComponent<LayoutElement>();

            inited = true;
        }

        public void Init(DialogueDisplayData displayData, TextAlignmentOptions alignment, Color characterNameColor,
            Color textColor, string materialName, DialogueEntryLayoutSetting layoutSetting, int textLeftExtraPadding)
        {
            InitReferences();
            this.displayData = displayData;
            this.alignment = alignment;
            this.characterNameColor = characterNameColor;
            this.textColor = textColor;
            this.materialName = materialName;
            this.layoutSetting = layoutSetting;
            this.textLeftExtraPadding = textLeftExtraPadding;
            UpdateText();
        }

        public void Clear()
        {
            displayData = null;
        }

        private void UpdateText()
        {
            if (!inited) return;
            if (displayData == null) return;
            contentBox.pageToDisplay = 1;
            content = I18n.__(displayData.dialogues);
            characterName = I18n.__(displayData.displayNames);
        }

        private void OnEnable()
        {
            UpdateText();
            I18n.LocaleChanged.AddListener(UpdateText);
        }

        private void OnDisable()
        {
            I18n.LocaleChanged.RemoveListener(UpdateText);
        }

        public bool Forward()
        {
            if (contentBox.pageToDisplay >= contentBox.textInfo.pageCount)
            {
                return false;
            }

            contentBox.pageToDisplay++;
            return true;
        }

        public string content
        {
            get => contentProxy.text;
            private set
            {
                contentProxy.text = value;
                // Update character count
                contentBox.ForceMeshUpdate();
            }
        }

        public string characterName
        {
            get => nameBox.text;
            private set => nameBox.text = value;
        }

        public TextAlignmentOptions alignment
        {
            get => contentBox.alignment;
            set => contentBox.alignment = value;
        }

        public Color characterNameColor
        {
            get => nameBox.color;
            set => nameBox.color = value;
        }

        public Color textColor
        {
            get => contentBox.color;
            set => contentBox.color = value;
        }

        public string materialName
        {
            get => contentProxy.materialName;
            set => contentProxy.materialName = value;
        }

        private DialogueEntryLayoutSetting _layoutSetting;

        public DialogueEntryLayoutSetting layoutSetting
        {
            get => _layoutSetting;
            set
            {
                _layoutSetting = value;
                var padding = verticalLayoutGroup.padding;
                padding.left = _layoutSetting.leftPadding + textLeftExtraPadding;
                padding.right = _layoutSetting.rightPadding;
                verticalLayoutGroup.spacing = _layoutSetting.nameTextSpacing;
                textLayoutElement.preferredHeight = _layoutSetting.preferredHeight;
            }
        }

        private int _textLeftExtraPadding;

        public int textLeftExtraPadding
        {
            get => _textLeftExtraPadding;
            set
            {
                _textLeftExtraPadding = value;
                verticalLayoutGroup.padding.left = layoutSetting.leftPadding + _textLeftExtraPadding;
            }
        }

        public bool canRefreshTextProxy
        {
            get => contentProxy.canRefreshLineBreak;
            set => contentProxy.canRefreshLineBreak = value;
        }

        public string FindIntersectingLink(Vector3 position, Camera camera)
        {
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(contentBox, position, camera);
            return linkIndex == -1 ? null : contentBox.textInfo.linkInfo[linkIndex].GetLinkID();
        }
    }
}
