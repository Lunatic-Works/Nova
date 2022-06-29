using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nova
{
    [RequireComponent(typeof(CompositeSpriteController))]
    public class CompositeSpriteRenderTarget : MonoBehaviour
    {
        private const string SUFFIX = "CharacterTexture";
        private const string SUBSUFFIX = "CharacterSubtexture";
        
        private CompositeSpriteController controller;
        private MyTarget target;
        private MyTarget subTarget;

        private void Awake()
        {
            controller = GetComponent<CompositeSpriteController>();
            target = new MyTarget(this, SUFFIX);
            target.Awake();
            subTarget = new MyTarget(this, SUBSUFFIX);
            subTarget.Awake();
        }

        private void Update()
        {
            target.Update();
            subTarget.Update();
        }

        private class MyTarget : RenderTarget
        {
            private CompositeSpriteRenderTarget parent;
            private string suffix;
            public override string textureName => parent == null ? oldConfig.name : parent.gameObject.name + suffix;
            public override bool isFinal => false;
            public override bool isActive => !string.IsNullOrEmpty(parent.controller.currentImageName);

            public MyTarget(CompositeSpriteRenderTarget parent, string suffix)
            {
                this.parent = parent;
                this.suffix = suffix;
            }
        }
    }
}
