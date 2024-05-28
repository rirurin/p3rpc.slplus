using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
using Reloaded.Hooks.Definitions;
using System.Runtime.InteropServices;

namespace p3rpc.slplus.SocialLink
{
    public class RankUpHooks : ModuleBase<SocialLinkContext>
    {

        [StructLayout(LayoutKind.Explicit, Size = 0xC30)]
        public unsafe struct AUICmmRankUpDraw
        {
            [FieldOffset(0x0000)] public AUIDrawBaseActor baseObj;
            [FieldOffset(0x02C0)] public USprAsset* pSprAsset;
            [FieldOffset(0x02C8)] public UPlgAsset* pPlgAsset;
            //[FieldOffset(0x0B10)] public UFrameBufferCapture* CaptureTexture;
            [FieldOffset(0x0B18)] public AUICmmRankUPAnimManager* AnimManager;
            [FieldOffset(0x0B20)] public USprAsset* pSprKeyHelp;
            [FieldOffset(0x0B28)] public USprAsset* pSprKeyHelpButton;
            [FieldOffset(0x0B30)] public AUIRankUpDraw* pManager;
            [FieldOffset(0x0C18)] public UUILayoutDataTable* OkNextLayoutDataTable;
            [FieldOffset(0x0C20)] public UUILayoutDataTable* OkNextMaskLayoutDataTable;
            [FieldOffset(0x0C28)] public UUILayoutDataTable* CmmRankUpLayoutDataTable;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x330)]
        public unsafe struct AUIRankUpDraw
        {
            [FieldOffset(0x0000)] public AUIBaseActor baseObj;
            //[FieldOffset(0x02B8)] public AUIPoetryActor* PoetryActor;
            //[FieldOffset(0x02C0)] public AUIArcanaCardCapture* UIACCaptureActor;
            //[FieldOffset(0x02C8)] public AUIGameOverPoem* UIGameOverPoem;
            [FieldOffset(0x02D0)] public UAssetLoader* pAssetLoader;
            [FieldOffset(0x02D8)] public UClass* UIBGActorClass;
            //[FieldOffset(0x02E0)] public AUICmmRankUpBG* pUIBGActor;
            [FieldOffset(0x02E8)] public UClass* UICmmRankUpDrawClass;
            [FieldOffset(0x02F0)] public AUICmmRankUpDraw* pUICmmRankUpDraw;
            [FieldOffset(0x02F8)] public UClass* RankUpAnimManagerClass;
            [FieldOffset(0x0300)] public AUICmmRankUPAnimManager* pRankUpAnimManager;
            [FieldOffset(0x0308)] public UMaterialInstance* pMaterialBGGameover;
            [FieldOffset(0x0318)] public UDataTable* OkNextLayoutData;
            [FieldOffset(0x0320)] public UDataTable* OkNextMaskLayoutData;
            [FieldOffset(0x0328)] public UDataTable* CmmRankUpLayoutData;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x480)]
        public unsafe struct AUICmmRankUPAnimManager
        {
            [FieldOffset(0x0000)] public AAppActor baseObj;
            [FieldOffset(0x0278)] public float RippleTime;
            [FieldOffset(0x027C)] public float RippleInitScale;
            [FieldOffset(0x0280)] public int CardNumFront;
            [FieldOffset(0x0284)] public int CardNumBack;
            [FieldOffset(0x0288)] public float CardScaleFront;
            [FieldOffset(0x028C)] public float CardScaleBack;
            [FieldOffset(0x0290)] public float TimeBGFadeIn;
            [FieldOffset(0x0294)] public float TimeBGFadeOut;
            [FieldOffset(0x0298)] public float TimeStarMainInAnime;
            [FieldOffset(0x029C)] public float FrameStartInAnimeInterval;
            [FieldOffset(0x02A0)] public float TimeWaitStarExpansion;
            [FieldOffset(0x02A4)] public float TimeWaitStarVanish;
            [FieldOffset(0x02A8)] public float TimeWaitStarWait;
            [FieldOffset(0x02AC)] public float TimeStartWaitAnimeInterval;
            [FieldOffset(0x02B0)] public float FrameCardFadeOut;
            [FieldOffset(0x02B4)] public FColor ColorNormalBG3Up;
            [FieldOffset(0x02B8)] public FColor ColorNormalBG3Middle;
            [FieldOffset(0x02BC)] public FColor ColorNormalBG3Down;
            [FieldOffset(0x02C0)] public FColor ColorNormalBG4;
            [FieldOffset(0x02C4)] public FColor ColorReverseBG3Up;
            [FieldOffset(0x02C8)] public FColor ColorReverseBG3Middle;
            [FieldOffset(0x02CC)] public FColor ColorReverseBG3Down;
            [FieldOffset(0x02D0)] public FColor ColorReverseBG4;
            [FieldOffset(0x02D4)] public float TimeMaxFadeIn;
            [FieldOffset(0x02D8)] public float TimeMaxWaitAfter;
            [FieldOffset(0x02E0)] public TArray<float> RippleBlur;
            [FieldOffset(0x02F0)] public TArray<float> RippleWidth;
            [FieldOffset(0x0300)] public float RationGradationUI;
            [FieldOffset(0x0304)] public float RationPosToOutGradationUI;
            [FieldOffset(0x0308)] public float RationReverseBG;
            [FieldOffset(0x030C)] public float ChangeRationReverseColorGradation;
            [FieldOffset(0x0310)] public float RotRationGradationReverse;
            [FieldOffset(0x0314)] public float AlphaRationBG12;
            [FieldOffset(0x0318)] public float SpeedRationBackCards;
            [FieldOffset(0x031C)] public float MoveRatioBackBGCard;
            [FieldOffset(0x0320)] public float MoveRatioFrontBGCard;
            [FieldOffset(0x0324)] public float ChangeRationReverseColorBackCards;
            [FieldOffset(0x0328)] public float RotationAllBGCard;
            [FieldOffset(0x032C)] public float RotRatioArcanaCard;
            [FieldOffset(0x0330)] public float AlphaRatioArcanaCard;
            [FieldOffset(0x0334)] public bool isVisibleArcanaCardShadow;
            [FieldOffset(0x0335)] public bool isVisibleStars;
            [FieldOffset(0x0338)] public float RotateAllStars;
            [FieldOffset(0x033C)] public float AlphaAllStars;
            [FieldOffset(0x0340)] public float MoveXRankUpTitle;
            [FieldOffset(0x0344)] public float AlphaRankUpTitle;
            [FieldOffset(0x0348)] public float RatioLetter;
            [FieldOffset(0x034C)] public float AlphaRankupStrings;
            [FieldOffset(0x0350)] public float MoveRationRankupStrings;
            [FieldOffset(0x0354)] public float AlphaRankupMaxStrings;
            [FieldOffset(0x0358)] public float MoveRankupMaxStrings;
            [FieldOffset(0x035C)] public bool IsChangeReverseSprCommuName;
            [FieldOffset(0x0360)] public float MoveXReverseString;
            [FieldOffset(0x0364)] public float AlphaReverseString;
            [FieldOffset(0x0368)] public float AlphaKeyhelp;
            [FieldOffset(0x036C)] public float ScaleKeyhelp;
            [FieldOffset(0x0370)] public float MoveAllKeyHelp;
            [FieldOffset(0x0374)] public float MoveMaskKeyHelp;
            [FieldOffset(0x0378)] public bool IsStartKeyHelpIn;
            [FieldOffset(0x0379)] public bool IsStartKeyHelpOut;
            [FieldOffset(0x037C)] public int AnimationContentGameOver;
            [FieldOffset(0x0380)] public float AlphaEFGameOver;
            [FieldOffset(0x0384)] public float ScaleRationGameOver;
            [FieldOffset(0x0388)] public float WeaveSpeedGameOver;
            [FieldOffset(0x038C)] public float ScaleWidthGameOver;
            [FieldOffset(0x0390)] public float ScaleHightGameOver;
            [FieldOffset(0x0394)] public float TimeGameOver;
            [FieldOffset(0x0398)] public float AlphaNormalGameOver;
            [FieldOffset(0x039C)] public float MoveYGameOver;
            [FieldOffset(0x03A0)] public float AlphaRipple1;
            [FieldOffset(0x03A4)] public float ScaleRipple1;
            [FieldOffset(0x03A8)] public float AlphaRipple2;
            [FieldOffset(0x03AC)] public float ScaleRipple2;
            [FieldOffset(0x03B0)] public float AlphaRipple3;
            [FieldOffset(0x03B4)] public float ScaleRipple3;
            [FieldOffset(0x03B8)] public bool IsEndFinalRipple;
            [FieldOffset(0x03BC)] public float AlphaCardEffect;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x3c)]
        public unsafe struct UUIContactManager_RankupMessagePayload
        {
            [FieldOffset(0x0)] public int MessageId;
            [FieldOffset(0x34)] public int ArcanaNo;
            [FieldOffset(0x38)] public int ModeNo;
        }

        private string AUICmmRankUpDraw_UICmmDrawLetter_SIG = "48 8B C4 48 89 58 ?? 48 89 70 ?? 48 89 78 ?? 55 41 56 41 57 48 8D 68 ?? 48 81 EC E0 00 00 00 83 B9 ?? ?? ?? ?? 0A";
        private IHook<AUICmmRankUpDraw_UICmmDrawLetter> _drawName;
        public unsafe delegate void AUICmmRankUpDraw_UICmmDrawLetter(AUICmmRankUpDraw* self, float x, float y);

        private string AUICmmRankUpDraw_UICmmDrawCard_SIG = "48 8B C4 55 57 41 54 48 8D 6C 24 ??";
        private IHook<AUICmmRankUpDraw_UICmmDrawLetter> _drawCard;
        public unsafe delegate void AUICmmRankUpDraw_UICmmDrawCard(AUICmmRankUpDraw* self, float x, float y);

        private string RankUpSendMessageSetArcanaNo_SIG = "48 83 EC 28 8B 42 ?? 89 41 ??";
        private IHook<RankUpSendMessageSetArcanaNo> _rankUpMessage;
        public unsafe delegate byte RankUpSendMessageSetArcanaNo(nint rankDraw /* + 0x278 */, UUIContactManager_RankupMessagePayload* msg);
        public unsafe RankUpHooks(SocialLinkContext context, Dictionary<string, ModuleBase<SocialLinkContext>> modules) : base(context, modules)
        {

        }
        public override void Register()
        {
        }
    }
}
