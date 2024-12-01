﻿/* Umbra | (c) 2024 by Una              ____ ___        ___.
 * Licensed under the terms of AGPL-3  |    |   \ _____ \_ |__ _______ _____
 *                                     |    |   //     \ | __ \\_  __ \\__  \
 * https://github.com/una-xiv/umbra    |    |  /|  Y Y  \| \_\ \|  | \/ / __ \_
 *                                     |______//__|_|  /____  /|__|   (____  /
 *     Umbra is free software: you can redistribute  \/     \/             \/
 *     it and/or modify it under the terms of the GNU Affero General Public
 *     License as published by the Free Software Foundation, either version 3
 *     of the License, or (at your option) any later version.
 *
 *     Umbra UI is distributed in the hope that it will be useful,
 *     but WITHOUT ANY WARRANTY; without even the implied warranty of
 *     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *     GNU Affero General Public License for more details.
 */

using System;
using System.Collections.Generic;
using Umbra.Common;
using Umbra.Game;
using Una.Drawing;

namespace Umbra.Widgets;

[ToolbarWidget("ExperienceBar", "Widget.ExperienceBar.Name", "Widget.ExperienceBar.Description")]
[ToolbarWidgetTags(["exp", "experience", "bar", "level"])]
internal partial class ExperienceBarWidget(
    WidgetInfo                  info,
    string?                     guid         = null,
    Dictionary<string, object>? configValues = null
) : ToolbarWidget(info, guid, configValues)
{
    public override WidgetPopup? Popup => null;

    private IPlayer Player { get; set; } = Framework.Service<IPlayer>();

    /// <inheritdoc/>
    protected override void Initialize() { }

    /// <inheritdoc/>
    protected override void OnUpdate()
    {
        if (Player.IsMaxLevel && !GetConfigValue<bool>("DisplayAtMaxLevel")) {
            Node.Style.IsVisible = false;
            return;
        }

        if (GetConfigValue<bool>("Decorate")) {
            Node.TagsList.Remove("ghost");
        } else {
            Node.TagsList.Add("ghost");
        }

        SanctuaryIconNode.Style.IsVisible = GetConfigValue<bool>("ShowSanctuaryIcon") && Player.IsInSanctuary;

        SyncIconNode.Style.IsVisible = GetConfigValue<bool>("ShowLevelSyncIcon")
            && Player.SyncedLevel > 0
            && Player.SyncedLevel != Player.Level;

        Node.Style.IsVisible     = true;
        Node.Tooltip             = GetTooltipText();
        LeftLabelNode.NodeValue  = GetLevelString();
        RightLabelNode.NodeValue = GetExpString();

        LeftLabelNode.Style.TextOffset     = new(0, GetConfigValue<int>("TextYOffset"));
        LeftLabelNode.Style.FontSize       = GetConfigValue<int>("TextSize");
        RightLabelNode.Style.TextOffset    = new(0, GetConfigValue<int>("TextYOffset"));
        RightLabelNode.Style.FontSize      = GetConfigValue<int>("TextSize");
        SanctuaryIconNode.Style.TextOffset = new(0, GetConfigValue<int>("MoonYOffset"));
        SyncIconNode.Style.TextOffset      = new(0, GetConfigValue<int>("SyncYOffset"));

        int fullWidth = GetConfigValue<int>("WidgetWidth")
            - 12
            - ((SyncIconNode.Style.IsVisible ?? true) || (SanctuaryIconNode.Style.IsVisible ?? true) ? 20 : 0);

        int leftWidth = LeftLabelNode.InnerWidth;

        SanctuaryIconNode.Style.Size = new(0, SafeHeight);
        SyncIconNode.Style.Size      = new(0, SafeHeight);
        LeftLabelNode.Style.Size     = new(0, SafeHeight);
        RightLabelNode.Style.Size    = new(fullWidth - (int)(leftWidth / Node.ScaleFactor) - 4, SafeHeight);

        UpdateVisualBars();
    }

    private string GetLevelString()
    {
        if (!GetConfigValue<bool>("ShowLevel")) return "";

        string syncLevel = $" ({Player.SyncedLevel})";
        string level     = I18N.Translate("Widget.GearsetSwitcher.JobLevel", Player.Level);

        return Player.SyncedLevel > 0 && Player.Level != Player.SyncedLevel ? $"{level}{syncLevel}" : level;
    }

    private string GetExpString()
    {
        if (!GetConfigValue<bool>("ShowExperience") || Player.IsMaxLevel) return "";

        if (GetConfigValue<bool>("ShowPreciseExperience")) {
            return GetPreciseExperienceString(false);
        }

        var xpPercent = Player.CurrentExperience / (float)Player.TotalRequiredExperience * 100;
        return $"{xpPercent:0.0}%";
    }

    private void UpdateVisualBars()
    {
        bool hasRested = Player.RestedExperience > 0;
        bool hasNormal = Player.CurrentExperience > 0;

        int  barWidth    = GetConfigValue<int>("WidgetWidth");
        int  maxWidth    = barWidth - 8;
        int  normalWidth = (int)(maxWidth * (Player.CurrentExperience / (float)(Player.TotalRequiredExperience)));
        uint restedXp    = Math.Min(Player.RestedExperience, Player.TotalRequiredExperience);

        int restedWidth = Math.Min(
            maxWidth - normalWidth,
            (int)(maxWidth * (restedXp / (float)(Player.TotalRequiredExperience)))
        );

        Node.Style.Size = new(barWidth, SafeHeight);

        if (hasNormal) {
            NormalXpBarNode.Style.IsVisible      = true;
            NormalXpBarNode.Style.Size           = new(normalWidth, SafeHeight - 8);
            RestedXpBarNode.Style.RoundedCorners = RoundedCorners.TopRight | RoundedCorners.BottomRight;
        } else {
            NormalXpBarNode.Style.IsVisible      = false;
            RestedXpBarNode.Style.RoundedCorners = null;
        }

        if (!hasRested) {
            RestedXpBarNode.Style.IsVisible      = false;
            NormalXpBarNode.Style.RoundedCorners = null;
            return;
        }

        RestedXpBarNode.Style.IsVisible      = true;
        RestedXpBarNode.Style.Size           = new(restedWidth, SafeHeight - 8);
        NormalXpBarNode.Style.RoundedCorners = RoundedCorners.TopLeft | RoundedCorners.BottomLeft;
    }

    private string? GetTooltipText()
    {
        return Player.IsMaxLevel ? null : $"{I18N.Translate("Experience")}: {GetPreciseExperienceString()}";
    }

    private string GetPreciseExperienceString(bool includeRested = true)
    {
        string currentXpStr = I18N.FormatNumber(Player.CurrentExperience);
        string neededXpStr  = I18N.FormatNumber(Player.TotalRequiredExperience);
        string restedXpStr  = I18N.FormatNumber(Player.RestedExperience);

        string restedStr = includeRested && Player.RestedExperience > 0
            ? $" - {I18N.Translate("Rested")}: {restedXpStr}"
            : "";

        return $"{currentXpStr} / {neededXpStr}{restedStr}";
    }
}
