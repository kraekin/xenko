﻿#define DEBUG_CanvasView

// Copyright (c) 2016 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Windows;
using System.Windows.Controls;

#if DEBUG_CanvasView
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Media;
using System.Windows.Threading;
#endif

namespace SiliconStudio.Presentation.Controls
{
    [TemplatePart(Name = CanvasPartName, Type = typeof(Canvas))]
    public class CanvasView : Control
    {
        /// <summary>
        /// The name of the part for the <see cref="Canvas"/>.
        /// </summary>
        private const string CanvasPartName = "PART_Canvas";

        /// <summary>
        /// The canvas.
        /// </summary>
        private Canvas canvas;
        /// <summary>
        /// The renderer.
        /// </summary>
        private CanvasRenderer renderer;

        static CanvasView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CanvasView), new FrameworkPropertyMetadata(typeof(CanvasView)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.canvas = GetTemplateChild(CanvasPartName) as Canvas;
            if (canvas == null)
                throw new InvalidOperationException($"A part named '{CanvasPartName}' must be present in the ControlTemplate, and must be of type '{typeof(Canvas).FullName}'.");

            this.canvas.UpdateLayout();
            this.renderer = new CanvasRenderer(this.canvas);
        }

#if DEBUG && DEBUG_CanvasView
        private const int UpdateInterval = 1000/60;

        private readonly Timer timer;
        private readonly Stopwatch watch = new Stopwatch();
        private readonly Func<double, double, double, double> function;

        private readonly object syncRoot = new object();

        private IList<Point>[] pointLists;

        private readonly Color[] colors = {
            Colors.Aqua,
            Colors.Bisque,
            Colors.Brown,
            Colors.Green,
            Colors.HotPink,
            Colors.Khaki,
            Colors.Maroon,
            Colors.Navy,
            Colors.Orange,
        };

        public CanvasView()
        {
            this.timer = new Timer(OnTimerElapsed);
            this.function = (t, x, a) => Math.Cos(t * a) * (x == 0 ? 1 : Math.Sin(x * a) / x);

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.timer.Change(Timeout.Infinite, Timeout.Infinite);
            pointLists = new IList<Point>[20];
            for (var i = 0; i < pointLists.Length; ++i)
            {
                pointLists[i] = new List<Point>();
            }
            this.watch.Start();
            this.timer.Change(1000, UpdateInterval);
        }

        private void OnTimerElapsed(object state)
        {
            lock (this.syncRoot)
            {
                var t = this.watch.ElapsedMilliseconds * 0.001;
                for (var i = 0; i < pointLists.Length; ++i)
                {
                    var points = pointLists[i];
                    var a = 0.5 + i * 0.05;
                    points.Clear();
                    for (double x = -5; x <= 5; x += 0.1)
                    {
                        points.Add(new Point(100*x + 500, 314*function(t, x, a) + 250));
                    }
                }
            }

            BeginInvoke(Render);
        }

        private void Render()
        {
            lock (this.syncRoot)
            {
                if (renderer == null)
                    return;
                renderer.Clear();
                for (var i = 0; i < pointLists.Length; ++i)
                {
                    //renderer.DrawPolygon(pointLists[i], colors[i%colors.Length], colors[(i+2) % colors.Length], 1, PenLineJoin.Bevel);
                    renderer.DrawPolyline(pointLists[i], colors[i%colors.Length], 1, PenLineJoin.Bevel);
                }
            }
        }

        private void BeginInvoke(Action action)
        {
            if (!this.Dispatcher.CheckAccess())
            {
                this.Dispatcher.BeginInvoke(DispatcherPriority.Background, action);
            }
            else
            {
                action();
            }
        }
#endif
    }
}
