using SharpGen.Runtime;
using System.Numerics;
using System.Windows;
using Vortice.Direct2D1;
using Vortice.Direct2D1.Effects;
using Windows.UI.Popups;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;

namespace ShiftCloneEffectPlugin
{
    internal class ShiftCloneEffectProcessor : IVideoEffectProcessor
    {
        readonly ShiftCloneEffect item;

        readonly AffineTransform2D transformEffect;

        readonly ID2D1Image? output;

        IGraphicsDevicesAndContext devices;

        ID2D1CommandList? commandList = null;

        ID2D1Image? input;

        bool isFirst = true;
        double dx, dy, dt, dt2;
        int max;

        int? amount = null;
        int? amount2 = null;
        DeleteModeEnum? mode = null;

        public ID2D1Image Output => output ?? input ?? throw new NullReferenceException();

        public ShiftCloneEffectProcessor(IGraphicsDevicesAndContext devices, ShiftCloneEffect item)
        {
            this.item = item;

            this.devices = devices;
            //Outputのインスタンスを固定するために、間にエフェクトを挟む
            transformEffect = new AffineTransform2D(devices.DeviceContext);
            output = transformEffect.Output;//EffectからgetしたOutputは必ずDisposeする必要がある。Effect側では開放されない。
        }

        /// <summary>
        /// エフェクトを更新する
        /// </summary>
        /// <param name="effectDescription">エフェクトの描画に必要な各種情報</param>
        /// <returns>描画位置等の情報</returns>
        public DrawDescription Update(EffectDescription effectDescription)
        {
            var frame = effectDescription.ItemPosition.Frame;
            var length = effectDescription.ItemDuration.Frame;
            var fps = effectDescription.FPS;

            var dx = item.X.GetValue(frame, length, fps);
            var dy = item.Y.GetValue(frame, length, fps);
            var dt = item.DeltaTime;
            var dt2 = item.DeleteTime;
            var max = item.Max;
            var mode = item.DeleteMode;
            //var fadein = item.FadeIn;
            //var fadeout = item.FadeOut;

            int num = frame * 1000 / fps;
            int amount = (dt > 0) ? (int)(num / dt + 1) : (max);
            if (amount > max) amount = max;

            int num2 = (length - frame) * 1000 / fps;
            int amount2 = (dt2 > 0) ? (int)(num2 / dt2 + 1) : (max);
            if (amount2 > max) amount2 = max;

            if (isFirst || this.dx != dx || this.dy != dy || this.dt != dt || this.dt2 != dt2 || this.max != max || this.amount != amount || this.mode != mode || this.amount2 != amount2)
            {
                commandList?.Dispose();//前回のUpdateで作成したCommandListを破棄する
                commandList = devices.DeviceContext.CreateCommandList();
                var dc = devices.DeviceContext;
                dc.Target = commandList;
                dc.BeginDraw();
                dc.Clear(null);
                if (input is not null)
                {
                    if ((int)mode < 1)
                    {
                        for (int i = max - amount2; i < amount; i++)
                            dc.DrawImage(input, new Vector2((float)dx * i, (float)dy * i), compositeMode: CompositeMode.SourceOver);
                    }
                    else
                    {
                        var amount3 = (amount < amount2) ? amount : amount2;
                        for (int i = 0; i < amount3; i++)
                            dc.DrawImage(input, new Vector2((float)dx * i, (float)dy * i), compositeMode: CompositeMode.SourceOver);
                    }
                }
                dc.EndDraw();
                commandList.Close();//CommandListはEndDraw()の後に必ずClose()を呼んで閉じる必要がある
                transformEffect.SetInput(0, commandList, true);
            }

            isFirst = false;
            this.dx = dx;
            this.dy = dy;
            this.dt = dt;
            this.dt2 = dt2;
            this.max = max;
            this.amount = amount;
            this.mode = mode;
            this.amount2 = amount2;

            return effectDescription.DrawDescription;
        }
        public void ClearInput()
        {
            input = null;
            transformEffect.SetInput(0, null, true);
        }
        public void SetInput(ID2D1Image input)
        {
            this.input = input;
        }

        public void Dispose()
        {
            commandList?.Dispose();//最後のUpdateで作成したCommandListを破棄
            transformEffect.SetInput(0, null, true);//EffectのInputは必ずnullに戻す。
            transformEffect.Dispose();
            output?.Dispose();//EffectからgetしたOutputは必ずDisposeする必要がある。Effect側では開放されない。
        }

    }
}
