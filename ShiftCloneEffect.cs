﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Effects;

namespace ShiftCloneEffectPlugin
{
    /// <summary>
    /// 映像エフェクト
    /// 映像エフェクトには必ず[VideoEffect]属性を設定してください。
    /// </summary>
    [VideoEffect("時間差複製エフェクト", new[] { "配置" }, new string[] { }, isAviUtlSupported:false)]
    public class ShiftCloneEffect : VideoEffectBase
    {
        /// <summary>
        /// エフェクトの名前
        /// </summary>
        public override string Label => "時間差複製エフェクト";

        /// <summary>
        /// アイテム編集エリアに表示するエフェクトの設定項目。
        /// [Display]と[AnimationSlider]等のアイテム編集コントロール属性の2つを設定する必要があります。
        /// [AnimationSlider]以外のアイテム編集コントロール属性の一覧はSamplePropertyEditorsプロジェクトを参照してください。
        /// </summary>
        [Display(GroupName = "描画", Name = "手前に複製", Description = "ON：クローンが手前に重なっていく。\nOFF：クローンが奥に重なっていく。")]
        [ToggleSlider]
        public bool Order { get => order; set => Set(ref order, value); }
        bool order = true;

        [Display(GroupName = "描画", Name = "X成分間隔", Description = "X成分間隔")]
        [AnimationSlider("F1", "px", -500, 500)]
        public Animation X { get; } = new Animation(0, -10000, 10000);

        [Display(GroupName = "描画", Name = "Y成分間隔", Description = "Y成分間隔")]
        [AnimationSlider("F1", "px", -500, 500)]
        public Animation Y { get; } = new Animation(0, -10000, 10000);

        [Display(GroupName = "描画", Name = "総表示個数", Description = "総表示個数")]
        [TextBoxSlider("F0", "個", 0, 10)]
        [DefaultValue(4)]
        [Range(1, 255)]
        public int Max { get => max; set => Set(ref max, value); }
        int max = 4;

        [Display(GroupName = "生成", Name = "生成時間間隔", Description = "生成時間間隔。\n0ミリ秒で単純複製します。")]
        [TextBoxSlider("F1", "ミリ秒", 0, 500)]
        [DefaultValue(0d)]
        [Range(0, 10000)]
        public float DeltaTime { get => deltaTime; set => Set(ref deltaTime, value); }
        float deltaTime = 0;

        [Display(GroupName = "生成", Name = "初期生成個数", Description = "初期生成個数")]
        [TextBoxSlider("F0", "個", 0, 10)]
        [DefaultValue(0)]
        [Range(0, 255)]
        public int BeginNum { get => beginNum; set => Set(ref beginNum, value); }
        int beginNum = 0;

        [Display(GroupName = "生成", Name = "フェードイン", Description = "フェードイン")]
        [TextBoxSlider("F2", "秒", 0, 1)]
        [DefaultValue(0d)]
        [Range(0, 10)]
        public float FadeIn { get => fadeIn; set => Set(ref fadeIn, value); }
        float fadeIn = 0;

        [Display(GroupName = "生成", Name = "初期半透明化", Description = "初期半透明化")]
        [ToggleSlider]
        public bool BeginOp { get => beginOp; set => Set(ref beginOp, value); }
        bool beginOp = false;
        
        [Display(GroupName = "消去", Name = "消去モード", Description = "消去モード")]
        [EnumComboBox]
        public DeleteModeEnum DeleteMode { get => deleteMode; set => Set(ref deleteMode, value); }
        DeleteModeEnum deleteMode = DeleteModeEnum.Straight;

        [Display(GroupName = "消去", Name = "消去時間間隔", Description = "消去時間間隔。\n0ミリ秒で消去なし。")]
        [TextBoxSlider("F1", "ミリ秒", 0, 500)]
        [DefaultValue(0d)]
        [Range(0, 10000)]
        public float DeleteTime { get => deleteTime; set => Set(ref deleteTime, value); }
        float deleteTime = 0;

        [Display(GroupName = "消去", Name = "終了直後個数", Description = "終了直後個数")]
        [TextBoxSlider("F0", "個", 0, 10)]
        [DefaultValue(0)]
        [Range(0, 255)]
        public int LeaveNum { get => leaveNum; set => Set(ref leaveNum, value); }
        int leaveNum = 0;

        [Display(GroupName = "消去", Name = "フェードアウト", Description = "フェードアウト")]
        [TextBoxSlider("F2", "秒", 0, 1)]
        [DefaultValue(0d)]
        [Range(0, 10)]
        public float FadeOut { get => fadeOut; set => Set(ref fadeOut, value); }
        float fadeOut = 0;

        [Display(GroupName = "消去", Name = "終了半透明化", Description = "終了半透明化")]
        [ToggleSlider]
        public bool EndOp { get => endOp; set => Set(ref endOp, value); }
        bool endOp = false;


        /// <summary>
        /// Exoフィルタを作成する。
        /// </summary>
        /// <param name="keyFrameIndex">キーフレーム番号</param>
        /// <param name="exoOutputDescription">exo出力に必要な各種情報</param>
        /// <returns></returns>
        public override IEnumerable<string> CreateExoVideoFilters(int keyFrameIndex, ExoOutputDescription exoOutputDescription)
        {
            //サンプルはSampleD2DVideoEffectを参照
            return Enumerable.Empty<string>();
        }


        /// <summary>
        /// 映像エフェクトを作成する
        /// </summary>
        /// <param name="devices">デバイス</param>
        /// <returns>映像エフェクト</returns>
        public override IVideoEffectProcessor CreateVideoEffect(IGraphicsDevicesAndContext devices)
        {
            return new ShiftCloneEffectProcessor(devices, this);
        }


        /// <summary>
        /// クラス内のIAnimatableを列挙する。
        /// </summary>
        /// <returns></returns>
        protected override IEnumerable<IAnimatable> GetAnimatables() => new[] { X, Y };
    }
}
