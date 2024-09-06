using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
using p3rpc.slplus.Hooking;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.Definitions.X64;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static p3rpc.slplus.SocialLink.SocialLinkManager;

namespace p3rpc.slplus.SocialLink
{
    public class RankUpHooks : ModuleBase<SocialLinkContext>
    {

        [StructLayout(LayoutKind.Explicit, Size = 0xC30)]
        public unsafe struct AUICmmRankUpDraw
        {
            [FieldOffset(0x0000)] public AUIDrawBaseActor baseObj;
            [FieldOffset(0x2b8)] public int Rank;
            [FieldOffset(0x02C0)] public USprAsset* pSprAsset;
            [FieldOffset(0x02C8)] public UPlgAsset* pPlgAsset;
            [FieldOffset(0x2d4)] public int ArcanaId;
            [FieldOffset(0x2e1)] public byte QueueId;
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

        // draw the correct sprite
        private string AUICmmRankUpDraw_UICmmDrawLetter_SIG = "48 8B C4 48 89 58 ?? 48 89 70 ?? 48 89 78 ?? 55 41 56 41 57 48 8D 68 ?? 48 81 EC E0 00 00 00 83 B9 ?? ?? ?? ?? 0A";
        private IHook<AUICmmRankUpDraw_UICmmDrawLetter> _drawName;
        public unsafe delegate void AUICmmRankUpDraw_UICmmDrawLetter(AUICmmRankUpDraw* self, float x, float y);

        private string AUICmmRankUpDraw_UICmmDrawCard_SIG = "48 8B C4 55 57 41 54 48 8D 6C 24 ??";
        private IHook<AUICmmRankUpDraw_UICmmDrawLetter> _drawCard;
        public unsafe delegate void AUICmmRankUpDraw_UICmmDrawCard(AUICmmRankUpDraw* self, float x, float y);

        // show the correct arcana
        private string RankUpSendMessageSetArcanaNo_SIG = "48 83 EC 28 8B 42 ?? 89 41 ??";
        private IHook<RankUpSendMessageSetArcanaNo> _rankUpMessage;
        public unsafe delegate byte RankUpSendMessageSetArcanaNo(nint rankDraw /* + 0x278 */, UUIContactManager_RankupMessagePayload* msg);

        // show the correct rank
        private string AUIRankUpDraw_CmmRankupInitGetRank_SIG = "48 8B 97 ?? ?? ?? ?? 49 8B 0E";
        private IAsmHook _cmmRankupInitGetRank;
        private IReverseWrapper<AUIRankUpDraw_CmmRankupInitGetRank> _cmmRankupInitGetRankWrapper;
        [Function(FunctionAttribute.Register.rax, FunctionAttribute.Register.rax, false)]
        public unsafe delegate AUICmmRankUpDraw* AUIRankUpDraw_CmmRankupInitGetRank(AUICmmRankUpDraw* self);

        private string UPlgAsset_DrawPlg1414e7820_SIG = "48 8B C4 F3 0F 11 58 ?? F3 0F 11 50 ?? 53 56 48 81 EC 58 01 00 00";
        private UPlgAsset_DrawPlg1414e7820 _drawPlg;
        public unsafe delegate void UPlgAsset_DrawPlg1414e7820(UPlgAsset* plg, int id, float x, float y, float z, FSprColor color, uint a7, float scaleX, float scaleY, float a10);

        private AssetLoader _assetLoader;
        private CommonHooks _common;
        private SocialLinkManager _manager;

        private int socialLinkNo;

        private static int[] arcanaNameSprId = // 0x14428a700
        {
            0x1F,          0x20,          0x21,          0x22,
            0x23,          0x24,          0x25,          0x26,
            0x27,          0x28,          0x29,          0x2A,
            0x2B,          0x2C,          0x2D,          0x2E,
            0x2F,          0x30,          0x31,          0x32,
            0x33,          0x34,          0x39,          0x3B,
            0x45
        };
        public unsafe RankUpHooks(SocialLinkContext context, Dictionary<string, ModuleBase<SocialLinkContext>> modules) : base(context, modules)
        {
            /*
            _context._utils.SigScan(RankUpSendMessageSetArcanaNo_SIG, "RankUpSendMessageSetArcanaNo", _context._utils.GetDirectAddress,
                addr => _rankUpMessage = _context._utils.MakeHooker<RankUpSendMessageSetArcanaNo>(RankUpSendMessageSetArcanaNoImpl, addr));

            _context._utils.SigScan(AUIRankUpDraw_CmmRankupInitGetRank_SIG, "AUIRankUpDraw::CmmRankupInitGetRank", _context._utils.GetDirectAddress, addr =>
            {
                string[] function =
                {
                    "use64",
                    $"{_context._utils.PreserveMicrosoftRegisters()}",
                    $"{_context._hooks.Utilities.GetAbsoluteCallMnemonics(AUIRankUpDraw_CmmRankupInitGetRankImpl, out _cmmRankupInitGetRankWrapper)}",
                    $"{_context._utils.RetrieveMicrosoftRegisters()}",
                };
                _cmmRankupInitGetRank = _context._hooks.CreateAsmHook(function, addr, AsmHookBehaviour.ExecuteFirst).Activate();
            });

            _context._utils.SigScan(AUICmmRankUpDraw_UICmmDrawLetter_SIG, "AUICmmRankUpDraw::UICmmDrawLetter", _context._utils.GetDirectAddress,
                addr => _drawName = _context._utils.MakeHooker<AUICmmRankUpDraw_UICmmDrawLetter>(AUICmmRankUpDraw_UICmmDrawLetterImpl, addr));
            _context._utils.SigScan(UPlgAsset_DrawPlg1414e7820_SIG, "UPlgAsset::DrawPlg1414e7820", _context._utils.GetDirectAddress,
                addr => _drawPlg = _context._utils.MakeWrapper<UPlgAsset_DrawPlg1414e7820>(addr));
            */
        }
        public override void Register()
        {
            _assetLoader = GetModule<AssetLoader>();
            _common = GetModule<CommonHooks>();
            _manager = GetModule<SocialLinkManager>();
        }

        public unsafe byte RankUpSendMessageSetArcanaNoImpl(nint pRankDraw, UUIContactManager_RankupMessagePayload* msg)
        {
            socialLinkNo = msg->ArcanaNo;
            if (_manager.cmmIndexToSlHash.TryGetValue(msg->ArcanaNo, out var slHash) && _manager.activeSocialLinks.TryGetValue(slHash, out var customSl))
                msg->ArcanaNo = (int)customSl.ArcanaId;
            return _rankUpMessage.OriginalFunction(pRankDraw, msg);
        }

        public unsafe AUICmmRankUpDraw* AUIRankUpDraw_CmmRankupInitGetRankImpl(AUICmmRankUpDraw* self)
        {
            if (_manager.cmmIndexToSlHash.TryGetValue(socialLinkNo, out var slHash) && _manager.activeSocialLinks.TryGetValue(slHash, out var customSl))
                self->Rank = 8; // TODO: make this an actual value
            return self;
        }

        public unsafe USprAsset* MakeRankupSprite(UTexture2D* headerTex)
        {
            USprAsset* newSpr = (USprAsset*)_context._objectMethods.SpawnObject("SprAsset", _context._objectMethods.GetEngineTransient());
            _context._objectMethods.MarkObjectAsRoot((UObject*)newSpr); // suppress GC
            _context._memoryMethods.TArray_Insert(&newSpr->mTexArray, (nint)headerTex);
            NativeMemory.Clear(&newSpr->SprDatas, 0x50);
            FSprDataArray newSprArr = new FSprDataArray();
            FSprData newSprEntry = new FSprData(512, 64, new FVector2D(0, 0), new FVector2D(1, 1), headerTex, uint.MaxValue, 0, 0);
            _context._memoryMethods.TArray_Insert(&newSprArr.SprDatas, newSprEntry);
            _context._memoryMethods.TMap_Insert(&newSpr->SprDatas, 0, newSprArr);
            _context._utils.Log($"rank up sprite: {(nint)newSpr:X}");
            return newSpr;
        }

        public unsafe void AUICmmRankUpDraw_UICmmDrawLetterImpl(AUICmmRankUpDraw* self, float x, float y)
        {
            if (self->Rank == 10)
            {
                // max (gay)
            }
            // rank name bg fade line
            var animManager = self->AnimManager;
            FColor black = new FColor((byte)animManager->AlphaRankupStrings, 0x0, 0x0, 0x0);
            _common._drawSpr(&self->baseObj.drawer, 
                x - 102 + self->AnimManager->MoveRationRankupStrings, 
                y + 102, 0, &black, 0x4a, 1, 1, 0, self->pSprAsset, EUI_DRAW_POINT.UI_DRAW_LEFT_TOP, self->QueueId
            );
            // rank number sprite 
            var rankSprId = (self->Rank != 0) ? self->Rank : 1;
            _drawPlg(self->pPlgAsset, rankSprId, x + animManager->MoveRationRankupStrings, y, 0, 
                new FSprColor(0xff, 0xff, 0xff, (byte)animManager->AlphaRankupStrings), 0xc, 1, 1, 0);
            // rank name sprite "RANK"
            var rankNameHeader = self->CmmRankUpLayoutDataTable->GetLayoutDataTableEntry(2);
            FColor white = new FColor((byte)animManager->AlphaRankupStrings, 0xff, 0xff, 0xff);
            _common._drawSpr(&self->baseObj.drawer,
                x + 222 + self->AnimManager->MoveRationRankupStrings + rankNameHeader->position.X,
                y + rankNameHeader->position.Y, 0, &white, 0x4b, 1, 1, 0, self->pSprAsset, EUI_DRAW_POINT.UI_DRAW_LEFT_TOP, self->QueueId
            );
            // arcana name
            var arcanaName = self->CmmRankUpLayoutDataTable->GetLayoutDataTableEntry(0);
            _common._drawSpr(&self->baseObj.drawer,
                x + self->AnimManager->MoveRationRankupStrings + arcanaName->position.X,
                y + arcanaName->position.Y, 0, &white, (uint)self->ArcanaId, 1, 1, 0, self->pSprAsset, EUI_DRAW_POINT.UI_DRAW_LEFT_TOP, self->QueueId
            );

            // social link name
            if (socialLinkNo < vanillaCmmLimit)
            {
                var slNameSprite = (uint)arcanaNameSprId[socialLinkNo - 1];
                _common._drawSpr(&self->baseObj.drawer,
                    x + 238 + self->AnimManager->MoveRationRankupStrings,
                    y + 124, 0, &white, slNameSprite, 1, 1, 0, self->pSprAsset, EUI_DRAW_POINT.UI_DRAW_LEFT_TOP, self->QueueId
                );
            }
            if (_manager.cmmIndexToSlHash.TryGetValue(socialLinkNo, out var slHash) && _manager.activeSocialLinks.TryGetValue(slHash, out var customSl))
            {
                var cmmWork = _common._getUGlobalWork()->pCommunityWork;
                if (customSl.RankUpName != null)
                {
                    var customCmm = &((CustomCmmData*)(cmmWork + 1))[socialLinkNo - vanillaCmmLimit - 1];
                    if (customCmm->RankUpName == null && customCmm->bRankUpNameLoading == 0)
                    {
                        _assetLoader.LoadAsset(cmmWork->pAssetLoader, Constants.MakeAssetPath($"{Constants.RankUpTextures}{customSl.RankUpName}"),
                            (nint)(&customCmm->RankUpName), x =>
                            {
                                _assetLoader.MarkAssetAsRoot(x);
                                UTexture2D* headerTex = *(UTexture2D**)x;
                                customCmm->RankUpSpr = MakeRankupSprite(headerTex);
                            });
                        customCmm->bRankUpNameLoading = 1;
                        _assetLoader._loadQueuedAssets(cmmWork->pAssetLoader);
                    }
                    if (customCmm->RankUpSpr != null)
                    {
                        _common._drawSpr(&self->baseObj.drawer,
                            x + 238 + self->AnimManager->MoveRationRankupStrings,
                            y + 124, 0, &white, 0, 1, 1, 0, customCmm->RankUpSpr, EUI_DRAW_POINT.UI_DRAW_LEFT_TOP, self->QueueId
                        );
                    }
                }
            }

            //_drawName.OriginalFunction(self, x, y);
        }
    }
}
