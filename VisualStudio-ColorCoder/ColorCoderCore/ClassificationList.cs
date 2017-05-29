﻿using System;
using System.Collections.Generic;
using System.Drawing;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace VisualStudio_ColorCoder.ColorCoderCore
{
    public class ClassificationList
    {
        private readonly ColorStorage _colorStorage;
        private readonly IDictionary<String, ColorableItemInfo[]> _classifications;
        private EnvDTE80.DTE2 dte;
        private FontsAndColorsItems _fontsAndColorsItems;

        public ClassificationList(ColorStorage colorStorage)
        {
            _colorStorage = colorStorage;
            _classifications = new Dictionary<String, ColorableItemInfo[]>();
            dte = (EnvDTE80.DTE2)System.Runtime.InteropServices.Marshal.GetActiveObject("VisualStudio.DTE.15.0");
            _fontsAndColorsItems = dte.Properties["FontsAndColors", "TextEditor"].Item("FontsAndColorsItems").Object as FontsAndColorsItems;
        }

        public void Load(params String[] classificationNames)
        {
            _classifications.Clear();

            Guid category = new Guid(Guids.TextEditorCategory);

            uint flags = (uint)(__FCSTORAGEFLAGS.FCSF_LOADDEFAULTS
                                 | __FCSTORAGEFLAGS.FCSF_NOAUTOCOLORS
                                 | __FCSTORAGEFLAGS.FCSF_READONLY);

            var hr = _colorStorage.Storage.OpenCategory(ref category, flags);
            ErrorHandler.ThrowOnFailure(hr);

            try
            {
                foreach (var classification in classificationNames)
                {
                    ColorableItemInfo[] colors = new ColorableItemInfo[1];

                    hr = _colorStorage.Storage.GetItem(classification, colors);
                    ErrorHandler.ThrowOnFailure(hr);

                    _classifications.Add(classification, colors);
                }
            }
            finally
            {
                _colorStorage.Storage.CloseCategory();
            }
        }

        //something
        public void Save()
        {
            Guid category = new Guid(Guids.TextEditorCategory);

            uint flags = (uint)(__FCSTORAGEFLAGS.FCSF_LOADDEFAULTS
                              | __FCSTORAGEFLAGS.FCSF_PROPAGATECHANGES);

            var hr = _colorStorage.Storage.OpenCategory(ref category, flags);
            ErrorHandler.ThrowOnFailure(hr);

            try
            {
                foreach (var classification in _classifications)
                {
                    hr = _colorStorage.Storage.SetItem(classification.Key, classification.Value);
                    ErrorHandler.ThrowOnFailure(hr);
                }
            }
            finally
            {
                _colorStorage.Storage.CloseCategory();
            }
        }

        public Color Get(String classificationName)
        {
            ColorableItemInfo[] entry;
            if (_classifications.TryGetValue(classificationName, out entry))
            {
                return ColorTranslator.FromWin32((int)entry[0].crForeground);
            }
            return default(Color);
        }

        public void Set(String classificationName, Color color)
        {
            Guid category = new Guid(Guids.TextEditorCategory);

            uint flags = (uint)(__FCSTORAGEFLAGS.FCSF_LOADDEFAULTS
                              | __FCSTORAGEFLAGS.FCSF_PROPAGATECHANGES);

            var hr = _colorStorage.Storage.OpenCategory(ref category, flags);
            ErrorHandler.ThrowOnFailure(hr);

            ColorableItemInfo[] colors = new ColorableItemInfo[1];
            colors[0].crForeground = (uint)ColorTranslator.ToWin32(color);
            colors[0].bForegroundValid = 1;
            _classifications[classificationName] = colors;

            try
            {
                hr = _colorStorage.Storage.SetItem(classificationName, colors);
                ErrorHandler.ThrowOnFailure(hr);
            }
            finally
            {
                _colorStorage.Storage.CloseCategory();
            }
        }

        public Color GetBuiltIn(String classificationName)
        {
            var colorItem = _fontsAndColorsItems.Item(classificationName);
            return ColorTranslator.FromWin32((int) colorItem.Foreground);
        }

        public void SetBuiltIn(String classificationName, Color color)
        {
            var colorItem = _fontsAndColorsItems.Item(classificationName);
            colorItem.Foreground = (uint)ColorTranslator.ToWin32(color);
        }
    }
}