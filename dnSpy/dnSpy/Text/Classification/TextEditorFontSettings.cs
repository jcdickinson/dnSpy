﻿/*
    Copyright (C) 2014-2016 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Media;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Text.Formatting;
using dnSpy.Contracts.Themes;

namespace dnSpy.Text.Classification {
	sealed class TextEditorFontSettings : ITextEditorFontSettings {
		readonly Lazy<TextEditorFormatDefinition, ITextEditorFormatDefinitionMetadata> textEditorFormatDefinition;
		readonly TextEditorFontSettings baseType;
		TextFormattingRunProperties textFormattingRunProperties;
		Brush windowBackgroundBrush;

		public TextEditorFontSettings(ITextEditorSettings textEditorSettings, Lazy<TextEditorFormatDefinition, ITextEditorFormatDefinitionMetadata> textEditorFormatDefinition, TextEditorFontSettings baseType) {
			this.textEditorFormatDefinition = textEditorFormatDefinition;
			this.baseType = baseType;
			if (baseType != null)
				baseType.SettingsChanged += BaseType_SettingsChanged;
			//TODO: Don't use the global settings
			textEditorSettings.PropertyChanged += TextEditorSettings_PropertyChanged;
		}

		void BaseType_SettingsChanged(object sender, EventArgs e) {
			//TODO: Only raise the event if we inherit the changed settings
			RaiseSettingsChanged(false);
		}

		void TextEditorSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			var settings = (ITextEditorSettings)sender;
			if (e.PropertyName == nameof(settings.FontFamily) || e.PropertyName == nameof(settings.FontSize))
				RaiseSettingsChanged(false);
		}

		public void OnThemeChanged() => RaiseSettingsChanged(true);

		void RaiseSettingsChanged(bool notifyBase) {
			textFormattingRunProperties = null;
			windowBackgroundBrush = null;
			// Need to make sure base has cleared its cache too or we could use old data in CreateTextFormattingRunProperties()
			if (notifyBase && baseType != null)
				baseType.RaiseSettingsChanged(notifyBase);
			SettingsChanged?.Invoke(this, EventArgs.Empty);
		}

		public event EventHandler SettingsChanged;

		public TextFormattingRunProperties CreateTextFormattingRunProperties(ITheme theme) {
			if (textFormattingRunProperties == null) {
				textFormattingRunProperties = textEditorFormatDefinition.Value.CreateTextFormattingRunProperties(theme);
				if (baseType != null)
					textFormattingRunProperties = TextFormattingRunPropertiesUtils.Merge(baseType.CreateTextFormattingRunProperties(theme), textFormattingRunProperties);
			}
			return textFormattingRunProperties;
		}

		public Brush GetWindowBackground(ITheme theme) {
			if (windowBackgroundBrush == null) {
				windowBackgroundBrush = textEditorFormatDefinition.Value.GetWindowBackground(theme);
				if (windowBackgroundBrush == null && baseType != null)
					windowBackgroundBrush = baseType.GetWindowBackground(theme);
				// Default settings instance should've returned a color
				Debug.Assert(windowBackgroundBrush != null);
				if (windowBackgroundBrush == null) {
					windowBackgroundBrush = theme.GetColor(ColorType.DefaultText).Background;
					Debug.Assert(windowBackgroundBrush != null);
					if (windowBackgroundBrush == null)
						windowBackgroundBrush = Brushes.White;
				}
			}
			return windowBackgroundBrush;
		}
	}
}