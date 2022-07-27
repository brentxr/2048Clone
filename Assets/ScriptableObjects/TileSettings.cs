using UnityEngine;

namespace ScriptableObjects
{
    [CreateAssetMenu(fileName = "TileSettings", menuName = "2048/Tile Settings", order = 0)]
    public class TileSettings : ScriptableObject
    {
        public float AnimationTime = 0.3f;
        public AnimationCurve AnimationCurve;
        public TileColor[] TileColors;
    }
}