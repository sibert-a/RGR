using RGR_TIMP_S4.Render;
using RGR_TIMP_S4.SortingCore;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RGR_TIMP_S4.SortingCore
{
    internal class AnimationPlayer
    {
        #region Константы анимации
        private const int VerticalSteps = 8;
        private const int HorizontalStepsBase = 10;
        private const int SwapSteps = 12;
        private const int FrameDelayMs = 5;
        #endregion

        #region Поля
        private readonly Panel canvas;
        private readonly SortingContext ctx;
        #endregion

        #region Инициализация
        public AnimationPlayer(Panel canvas, SortingContext ctx)
        {
            this.canvas = canvas;
            this.ctx = ctx;
        }
        #endregion

        #region Публичные методы управления
        public void InvalidateCanvas() => canvas.Invalidate();

        public (int x, int y) GetPositionOnCanvas(int index) =>
            GeometryHelper.GetElementScreenPosition(ctx.Array.Length, canvas.ClientSize, index);
        #endregion

        #region Основные анимации
        // Полёт двух элементов к сравнению
        public async Task FlyToComparisonAsync(int index1, int index2)
        {
            SetExternalIndices(index1, index2);

            var start1 = GetElementPosition(index1);
            var start2 = GetElementPosition(index2);
            var target = GeometryHelper.GetComparisonTargetPosition(ctx.Array.Length, canvas.ClientSize, index1, index2);

            SetFlyingValues(ctx.Array[index1], ctx.Array[index2]);
            SetFlyingState(true, true);

            // Подъём вверх (оба элемента)
            await AnimateLiftTwo(start1.x, start1.y, target.y1,
                                 start2.x, start2.y, target.y2);
            // Горизонтальное сближение
            await AnimateApproachTwo(start1.x, start2.x, target.x1, target.x2,
                                     target.y1, target.y2, index1, index2);

            // Фиксация статичного положения для сравнения
            SetFlyingState(false, false);
            SetExternalElements(ctx.Array[index1], ctx.Array[index2]);
            InvalidateCanvas();
        }

        // Возврат элементов на место
        public async Task FlyBackAsync()
        {
            if (!ctx.ExternalElement1.HasValue && !ctx.ExternalElement2.HasValue) return;

            int? idx1 = ctx.ExternalElementIndex1;
            int? idx2 = ctx.ExternalElementIndex2;
            int? val1 = ctx.ExternalElement1;
            int? val2 = ctx.ExternalElement2;

            if (idx1.HasValue && idx2.HasValue)
            {
                // Текущая позиция в зоне сравнения
                var curTarget = GeometryHelper.GetComparisonTargetPosition(ctx.Array.Length, canvas.ClientSize, idx1.Value, idx2.Value);
                // Исходная позиция в основном массиве
                var home1 = GetElementPosition(idx1.Value);
                var home2 = GetElementPosition(idx2.Value);

                SetFlyingValues(val1.Value, val2.Value);
                SetFlyingState(true, true);
                ClearExternalElements();
                ctx.ComparisonSign = "";
                InvalidateCanvas();

                // Горизонтальное расхождение к своим колонкам
                await AnimateApproachTwo(curTarget.x1, curTarget.x2, home1.x, home2.x,
                                         curTarget.y1, curTarget.y2, idx1.Value, idx2.Value);
                // Падение вниз (оба элемента)
                await AnimateLandTwo(home1.x, curTarget.y1, home1.y,
                                     home2.x, curTarget.y2, home2.y);
            }
            else if (idx1.HasValue)
            {
                var home = GetElementPosition(idx1.Value);
                int curY = home.y - GeometryHelper.VerticalComparisonOffset;

                SetFlyingValues(val1.Value, 0);
                SetFlyingState(true, false);
                ClearExternalSingle();
                ctx.ComparisonSign = "";
                InvalidateCanvas();

                // Падение одного элемента
                await AnimateLandSingle(home.x, curY, home.y);
            }

            SetFlyingState(false, false);
            ClearExternalIndices();
            InvalidateCanvas();
        }

        // Обмен двух элементов наверху
        public async Task SwapOnTopAsync(int index1, int index2)
        {
            var target = GeometryHelper.GetComparisonTargetPosition(ctx.Array.Length, canvas.ClientSize, index1, index2);
            int x1 = target.x1, y1 = target.y1;
            int x2 = target.x2, y2 = target.y2;

            int val1 = ctx.Array[index1];
            int val2 = ctx.Array[index2];

            ClearExternalElements();
            ctx.ComparisonSign = "";
            SetFlyingValues(val1, val2);
            SetFlyingState(true, true);
            // Начальные координаты на месте сравнения
            SetFlyPositions(x1, y1, x2, y2);
            InvalidateCanvas();

            // Анимация встречного движения
            float fromX1 = x1, fromY1 = y1;
            float fromX2 = x2, fromY2 = y2;
            for (int step = 0; step <= SwapSteps; step++)
            {
                float t = (float)step / SwapSteps;
                SetFlyPositions(
                    fromX1 + (fromX2 - fromX1) * t,
                    fromY1 + (fromY2 - fromY1) * t,
                    fromX2 + (fromX1 - fromX2) * t,
                    fromY2 + (fromY1 - fromY2) * t);
                InvalidateCanvas();
                await Task.Delay(FrameDelayMs);
            }

            SetFlyingState(false, false);
            SetExternalElements(val1, val2);
            SetExternalIndices(index1, index2);
            InvalidateCanvas();
        }

        // Показ одного элемента над массивом
        public async Task SingleFlyToComparisonAsync(int index)
        {
            SetExternalIndices(index, null);

            var start = GetElementPosition(index);
            int targetY = start.y - GeometryHelper.VerticalComparisonOffset;

            SetFlyingValues(ctx.Array[index], 0);
            SetFlyingState(true, false);

            await AnimateLiftSingle(start.x, start.y, targetY);

            SetFlyingState(false, false);
            SetExternalSingle(ctx.Array[index]);
            InvalidateCanvas();
        }

        // Перемещение элемента в заданные координаты (слияние)
        public async Task FlyToPositionAsync(int sourceIndex, int targetX, int targetY)
        {
            var start = GetElementPosition(sourceIndex);

            SetExternalIndices(sourceIndex, null);
            SetFlyingValues(ctx.Array[sourceIndex], 0);
            SetFlyingState(true, false);

            int liftY = start.y - GeometryHelper.VerticalComparisonOffset;

            // Подъём
            await AnimateLiftSingle(start.x, start.y, liftY);
            // Горизонтальный перелёт
            await AnimateHorizontalMove(start.x, targetX, liftY);
            // Спуск на целевую позицию
            await AnimateLandSingle(targetX, liftY, targetY);

            SetFlyingState(false, false);
            ClearExternalSingle();
            ClearExternalIndices();
            InvalidateCanvas();
        }
        #endregion

        #region Приватные хелперы для визуального состояния
        private void SetExternalIndices(int? idx1, int? idx2)
        {
            ctx.ExternalElementIndex1 = idx1;
            ctx.ExternalElementIndex2 = idx2;
        }

        private void ClearExternalIndices()
        {
            ctx.ExternalElementIndex1 = null;
            ctx.ExternalElementIndex2 = null;
        }

        private void SetFlyingValues(int val1, int val2)
        {
            ctx.FlyingValue1 = val1;
            ctx.FlyingValue2 = val2;
        }

        private void SetFlyingState(bool fly1, bool fly2)
        {
            ctx.IsFlying1 = fly1;
            ctx.IsFlying2 = fly2;
        }

        private void SetFlyPositions(float x1, float y1, float x2, float y2)
        {
            ctx.FlyX1 = x1;
            ctx.FlyY1 = y1;
            ctx.FlyX2 = x2;
            ctx.FlyY2 = y2;
        }

        private void SetExternalElements(int? val1, int? val2)
        {
            ctx.ExternalElement1 = val1;
            ctx.ExternalElement2 = val2;
        }

        private void SetExternalSingle(int? val)
        {
            ctx.ExternalElement1 = val;
            ctx.ExternalElement2 = null;
        }

        private void ClearExternalElements()
        {
            ctx.ExternalElement1 = null;
            ctx.ExternalElement2 = null;
        }

        private void ClearExternalSingle()
        {
            ctx.ExternalElement1 = null;
        }

        private (int x, int y) GetElementPosition(int index) =>
            GeometryHelper.GetElementScreenPosition(ctx.Array.Length, canvas.ClientSize, index);
        #endregion

        #region Приватные анимационные хелперы (базовые движения)
        // Вертикальный подъём двух элементов (ease out)
        private async Task AnimateLiftTwo(float x1, float y1, float toY1, float x2, float y2, float toY2)
        {
            for (int step = 0; step <= VerticalSteps; step++)
            {
                float t = (float)step / VerticalSteps;
                float ease = 1 - (float)Math.Pow(1 - t, 2);
                SetFlyPositions(x1, y1 + (toY1 - y1) * ease,
                                x2, y2 + (toY2 - y2) * ease);
                await FrameUpdate();
            }
        }

        // Горизонтальное сближение/расхождение двух элементов (ease in)
        private async Task AnimateApproachTwo(float fromX1, float fromX2, float toX1, float toX2,
                                              float y1, float y2, int idx1, int idx2)
        {
            int steps = Math.Max(Math.Abs(idx1 - idx2), HorizontalStepsBase);
            for (int step = 0; step <= steps; step++)
            {
                float t = (float)step / steps;
                float ease = 1 - (1 - t) * (1 - t);
                SetFlyPositions(fromX1 + (toX1 - fromX1) * ease, y1,
                                fromX2 + (toX2 - fromX2) * ease, y2);
                await FrameUpdate();
            }
        }

        // Вертикальный спуск двух элементов (ease in)
        private async Task AnimateLandTwo(float x1, float fromY1, float toY1, float x2, float fromY2, float toY2)
        {
            for (int step = 0; step <= VerticalSteps; step++)
            {
                float t = (float)step / VerticalSteps;
                float ease = t * t;
                SetFlyPositions(x1, fromY1 + (toY1 - fromY1) * ease,
                                x2, fromY2 + (toY2 - fromY2) * ease);
                await FrameUpdate();
            }
        }

        // Вертикальный подъём одного элемента (ease out)
        private async Task AnimateLiftSingle(float x, float fromY, float toY)
        {
            for (int step = 0; step <= VerticalSteps; step++)
            {
                float t = (float)step / VerticalSteps;
                float ease = 1 - (float)Math.Pow(1 - t, 2);
                SetFlyPositions(x, fromY + (toY - fromY) * ease, 0, 0);
                await FrameUpdate();
            }
        }

        // Горизонтальный перелёт одного элемента (ease in)
        private async Task AnimateHorizontalMove(float fromX, float toX, float y)
        {
            int steps = HorizontalStepsBase;
            for (int step = 0; step <= steps; step++)
            {
                float t = (float)step / steps;
                float ease = 1 - (1 - t) * (1 - t);
                SetFlyPositions(fromX + (toX - fromX) * ease, y, 0, 0);
                await FrameUpdate();
            }
        }

        // Вертикальный спуск одного элемента (ease in)
        private async Task AnimateLandSingle(float x, float fromY, float toY)
        {
            for (int step = 0; step <= VerticalSteps; step++)
            {
                float t = (float)step / VerticalSteps;
                float ease = t * t;
                SetFlyPositions(x, fromY + (toY - fromY) * ease, 0, 0);
                await FrameUpdate();
            }
        }

        // Обновление кадра: перерисовка и задержка
        private async Task FrameUpdate()
        {
            InvalidateCanvas();
            await Task.Delay(FrameDelayMs);
        }
        #endregion
    }
}