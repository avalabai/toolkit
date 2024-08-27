using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AI.Avalab.Toolkit
{
    [CreateAssetMenu(fileName = "manifest", menuName = "Avatar Manifest")]
    public class AvatarManifest : ScriptableObject
    {
        public Animator rootAnimator;
    }
}
