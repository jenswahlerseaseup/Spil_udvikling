using UnityEngine;

// Holds all directional sprites for one character. This keeps art data reusable
// and lets the player animator stay independent from a specific sprite source.
[CreateAssetMenu(menuName = "Nyt Spil/Character/Sprite Set")]
public sealed class CharacterSpriteSet : ScriptableObject
{
    [Header("Front - facing toward camera")]
    public Sprite frontIdle;
    public Sprite[] frontWalk;
    public Sprite frontJump;

    [Header("Back - facing away from camera")]
    public Sprite backIdle;
    public Sprite[] backWalk;
    public Sprite backJump;

    [Header("Side Right")]
    public Sprite sideRightIdle;
    public Sprite[] sideRightWalk;
    public Sprite sideRightJump;

    [Header("Side Left")]
    public Sprite sideLeftIdle;
    public Sprite[] sideLeftWalk;
    public Sprite sideLeftJump;
}
