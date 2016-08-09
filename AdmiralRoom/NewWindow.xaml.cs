﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Huoyaoyuan.AdmiralRoom.Composition;
using Xceed.Wpf.AvalonDock.Layout;
using Xceed.Wpf.AvalonDock.Layout.Serialization;

namespace Huoyaoyuan.AdmiralRoom
{
    /// <summary>
    /// NewWindow.xaml 的交互逻辑
    /// </summary>
    public partial class NewWindow : Window
    {
        public NewWindow()
        {
            InitializeComponent();
            ResourceService.Current.CultureChanged += OnCultureChanged;

            foreach (var subview in ModuleHost.Instance.SubViews)
            {
                AddOrShowView(subview, false);
                var menuitem = new MenuItem
                {
                    Header = subview.GetTitle(ResourceService.Current.CurrentCulture)
                };
                menuitem.Click += (_, __) => AddOrShowView(subview, true);
                ResourceService.Current.CultureChanged += c => menuitem.Header = subview.GetTitle(c);
            }

            foreach (var subwindow in ModuleHost.Instance.SubWindows)
            {
                var menuitem = new MenuItem
                {
                    Header = subwindow.GetTitle(ResourceService.Current.CurrentCulture)
                };
                var closure = new SubWindowClosure(menuitem, subwindow);
                menuitem.Click += closure.Click;
                ResourceService.Current.CultureChanged += closure.OnCultureChanged;
            }
        }

        private class SubWindowClosure
        {
            private MenuItem _menuitem;
            private ISubWindow _isubwindow;
            private Window _window;
            public SubWindowClosure(MenuItem menuitem, ISubWindow isubwindow)
            {
                _menuitem = menuitem;
                _isubwindow = isubwindow;
            }
            public void Click(object sender, RoutedEventArgs e)
            {
                if (_window == null)
                {
                    _window = _isubwindow.CreateWindow();
                    _window.Closed += WindowClosed;
                }
                _window.Show();
                _window.Activate();
            }
            private void WindowClosed(object sender, EventArgs e) => _window = null;
            public void OnCultureChanged(CultureInfo culture) => _menuitem.Header = _isubwindow.GetTitle(culture);
        }

        private void MakeViewList(ILayoutElement elem)
        {
            if (elem is LayoutContent)
            {
                viewList[(elem as LayoutContent).ContentId] = elem as LayoutContent;
                return;
            }
            if (elem is ILayoutContainer)
                foreach (var child in (elem as ILayoutContainer).Children)
                    MakeViewList(child);
        }

        #region Layout
        private void LoadLayout(object sender, RoutedEventArgs e)
        {
            TryLoadLayout();
            foreach (var view in DockMan.Layout.Hidden.Where(x => x.PreviousContainerIndex == -1).ToArray())
                DockMan.Layout.Hidden.Remove(view);
        }
        private void SaveLayout(object sender, EventArgs e) => TrySaveLayout();
        private void TryLoadLayout(string path = "layout.xml")
        {
            XmlLayoutSerializer layoutserializer = new XmlLayoutSerializer(DockMan);
            layoutserializer.LayoutSerializationCallback += (_, args) => args.Model.Content = args.Content;
            try
            {
                layoutserializer.Deserialize(path);
            }
            catch { }
            MakeViewList(DockMan.Layout);
        }
        private void TrySaveLayout(string path = "layout.xml")
        {
            XmlLayoutSerializer layoutserializer = new XmlLayoutSerializer(DockMan);
            try
            {
                layoutserializer.Serialize(path);
            }
            catch { }
        }
        #endregion

        private readonly Dictionary<string, LayoutContent> viewList = new Dictionary<string, LayoutContent>();
        private readonly Dictionary<string, ISubView> subviewmap = new Dictionary<string, ISubView>();
        private void AddOrShowView(ISubView view, bool show)
        {
            subviewmap[view.ContentID] = view;
            string viewname = view.ContentID;
            LayoutContent targetContent;
            LayoutAnchorable targetView;
            viewList.TryGetValue(viewname, out targetContent);
            targetView = targetContent as LayoutAnchorable;
            if (targetView == null)
            {
                targetView = new LayoutAnchorable();
                viewList.Add(viewname, targetView);
                targetView.AddToLayout(DockMan, AnchorableShowStrategy.Most);
                targetView.DockAsDocument();
                targetView.CanClose = false;
                targetView.Hide();
            }
            if (targetView.Content == null)
            {
                var content = view.View;
                targetView.Content = content;
                targetView.ContentId = viewname;
                targetView.Title = view.GetTitle(ResourceService.Current.CurrentCulture);
                targetView.CanAutoHide = true;
            }
            if (show) targetView.IsVisible = true;
        }

        private void OnCultureChanged(CultureInfo culture)
        {
            foreach (var viewname in viewList.Keys)
            {
                var target = viewList[viewname];
                ISubView view;
                if (subviewmap.TryGetValue(viewname, out view))
                    target.Title = view.GetTitle(culture);
            }
        }

        private void SetBrowserZoomFactor(object sender, RoutedPropertyChangedEventArgs<double> e)
            => this.GameHost.ApplyZoomFactor(e.NewValue);
    }
}
