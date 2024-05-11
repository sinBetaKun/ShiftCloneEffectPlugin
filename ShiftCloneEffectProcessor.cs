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
        readonly List<Opacity> opacityEffects;

        readonly ID2D1Image? output;

        IGraphicsDevicesAndContext devices;

        ID2D1CommandList? commandList = null;

        ID2D1Image? input;

        bool isFirst = true;
        bool order = true;
        bool beginOp = false;
        bool endOp = false;
        double dx, dy, dt, dt2;
        int max , beginNum, leaveNum;
        bool fadeUpdate = false;

        int? amount = null;
        int? amount2 = null;
        int leftTime = 0;
        int rightTime = 0;
        DeleteModeEnum? mode = null;

        float fadein, fadeout;

        public ID2D1Image Output => output ?? input ?? throw new NullReferenceException();

        public ShiftCloneEffectProcessor(IGraphicsDevicesAndContext devices, ShiftCloneEffect item)
        {
            this.item = item;

            this.devices = devices;
            //Outputのインスタンスを固定するために、間にエフェクトを挟む
            transformEffect = new AffineTransform2D(devices.DeviceContext);
            opacityEffects = new List<Opacity> { };
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
            var order = item.Order;
            var mode = item.DeleteMode;
            var fadein = item.FadeIn;
            var fadeout = item.FadeOut;
            var beginNum = item.BeginNum;
            var leaveNum = item.LeaveNum;
            var beginOp = item.BeginOp;
            var endOp = item.EndOp;

            leftTime = frame * 1000 / fps;
            int amount = (dt > 0) ? (int)(leftTime / dt + 1 + beginNum) : (max);
            if (amount > max) amount = max;

            rightTime = (length - frame) * 1000 / fps;
            int amount2 = (dt2 > 0) ? (int)(rightTime / dt2 + 1 + leaveNum) : (max);
            if (amount2 > max) amount2 = max;
            var fadeUpdate = (((dt * (max - beginNum - 1) / 1000) + fadein) * fps > frame) || (((dt2 * (max - leaveNum - 1) / 1000) + fadeout) * fps > length - frame);

            if (isFirst || fadeUpdate || this.fadeUpdate || this.dx != dx || this.dy != dy || this.dt != dt || this.dt2 != dt2 || this.max != max || this.amount != amount || this.mode != mode || this.amount2 != amount2 || this.fadein != fadein || this.fadeout != fadeout || this.order != order || this.beginNum != beginNum || this.leaveNum != leaveNum || this.beginOp != beginOp || this.endOp != endOp)
            {
                commandList?.Dispose();//前回のUpdateで作成したCommandListを破棄する
                this.dx = dx;
                this.dy = dy;
                this.dt = dt;
                this.dt2 = dt2;
                this.max = max;
                this.amount = amount;
                this.mode = mode;
                this.amount2 = amount2;
                this.fadein = fadein;
                this.fadeout = fadeout;
                this.order = order;
                this.beginNum = beginNum;
                this.leaveNum = leaveNum;
                this.beginOp = beginOp;
                this.endOp = endOp;
                this.fadeUpdate = fadeUpdate;
                SetCommandList();
            }

            isFirst = false;

            return effectDescription.DrawDescription;
        }

        public void ClearInput()
        {
            input = null;
            transformEffect.SetInput(0, null, true);
            commandList?.Dispose();//前回のUpdateで作成したCommandListを破棄する
            DisposeOpacityEffects(0);
        }


        public void SetInput(ID2D1Image input)
        {
            this.input = input;
            SetCommandList();
        }

        public void Dispose()
        {
            commandList?.Dispose();//最後のUpdateで作成したCommandListを破棄
            transformEffect.SetInput(0, null, true);//EffectのInputは必ずnullに戻す。
            transformEffect.Dispose();
            DisposeOpacityEffects(0);
            output?.Dispose();//EffectからgetしたOutputは必ずDisposeする必要がある。Effect側では開放されない。
        }

        public void DisposeOpacityEffects(int count)
        {
            while (opacityEffects.Count > count)
            {
                opacityEffects[count].SetInput(0, null, true);
                opacityEffects[count].Dispose();
                opacityEffects.RemoveAt(count);
            }
        }

        public void SetCommandList()
        {
            commandList = devices.DeviceContext.CreateCommandList();
            var dc = devices.DeviceContext;
            dc.Target = commandList;
            dc.BeginDraw();
            dc.Clear(null);
            if (input is not null && mode is not null && amount is not null && amount2 is not null)
            {
                var amount3 = ((int)mode < 1) ? (max - (int)amount2) : 0;
                var amount4 = (int)(((int)mode < 1) ? amount : ((amount < amount2) ? amount : amount2));
                Func<int, double> calcNum3 = (int x) => (leftTime - dt * (x - beginNum)) / fadein / 1000;
                Func<int, double> calcNum4;

                if ((int)mode < 1)
                    calcNum4 = (int x) => (rightTime - dt2 * (max - 1 - x - leaveNum)) / fadeout / 1000;
                else
                    calcNum4 = (int x) => (rightTime - dt2 * (x - leaveNum)) / fadeout / 1000;

                Func<int, bool> range = (order) ? (int x) => x < amount4 : (int x) => x >= amount3;
                Func<int, bool> range2 = ((int)mode < 1) ? (int x) => x >= max - leaveNum : (int x) => x < leaveNum;
                int adder = (order) ? 1 : -1;
                var fadable = 0;

                for (int i = (order) ? amount3 : amount4 - 1; range(i); i += adder)
                {
                    var num3 = (fadein > 0 && !(!beginOp && beginNum >= i)) ? calcNum3(i) : 1;
                    var num4 = (fadeout > 0 && !(!endOp && range2(i))) ? calcNum4(i) : 1;
                    var num5 = (num3 < num4) ? num3 : num4;
                    if (num5 < 1)
                    {
                        if (opacityEffects.Count <= fadable) opacityEffects.Add(new Opacity(devices.DeviceContext));
                        var opacityEffect = opacityEffects[fadable];
                        opacityEffect.SetInput(0, input, true);
                        opacityEffect.Value = (float)num5;
                        dc.DrawImage(opacityEffect.Output, new Vector2((float)dx * i, (float)dy * i), compositeMode: CompositeMode.SourceOver);
                        fadable++;
                    }
                    else
                        dc.DrawImage(input, new Vector2((float)dx * i, (float)dy * i), compositeMode: CompositeMode.SourceOver);
                }
                DisposeOpacityEffects(fadable);
            }
            dc.EndDraw();
            commandList.Close();//CommandListはEndDraw()の後に必ずClose()を呼んで閉じる必要がある
            transformEffect.SetInput(0, commandList, true);
        }
    }
}
