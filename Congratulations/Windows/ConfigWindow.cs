﻿using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using ImGuiNET;

namespace Congratulations.Windows;

public class ConfigWindow : Window, IDisposable
{
    public static readonly String Title = "Congratulations Configuration";
    
    private readonly Configuration configuration;

    private readonly FileDialogManager dialogManager;

    public ConfigWindow(CongratulationsPlugin congratulationsPlugin) : base(
        Title,
        ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
        ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.Size = new Vector2(500, 500);
        this.SizeCondition = ImGuiCond.Appearing;

        this.configuration = congratulationsPlugin.Configuration;
        dialogManager = new FileDialogManager { AddedWindowFlags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking};
        dialogManager.CustomSideBarItems.Add((Environment.ExpandEnvironmentVariables("%USERNAME%"), Environment.ExpandEnvironmentVariables("%USERPROFILE%"), FontAwesomeIcon.User, 0));
    }
    
    public override void Draw()
    {
        DrawSection(configuration.OneThird);
        DrawSection(configuration.TwoThirds);
        DrawSection(configuration.ThreeThirds);
        DrawSection(configuration.AllSevenInAFullParty);
        dialogManager.Draw();

        if (ImGui.Button("Save"))
        {
            configuration.Save();
        }
    }

    private void DrawSection(Configuration.SubConfiguration config)
    {
        if (!ImGui.TreeNode(config.SectionTitle)) return;
        var playSound = config.PlaySound;
        if (ImGui.Checkbox("Play sound", ref playSound))
        {
            config.PlaySound = playSound;
        }

        if (config.PlaySound)
        {
                    var volume = config.Volume;
                    if (ImGui.SliderInt("Volume", ref volume, 0, 100))
                    {
                        config.Volume = volume;
                    }
                    
                    ImGui.SameLine();
                    if (ImGuiComponents.IconButton(FontAwesomeIcon.Play))
                    {
                        SoundEngine.PlaySound(config.getFilePath(), config.Volume * 0.01f);
                    }
            
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Preview sound on current volume");
                    }
            
                    
                    var useCustomSound = config.UseCustomSound;
                    if (ImGui.Checkbox("Use custom sound", ref useCustomSound))
                    {
                        config.UseCustomSound = useCustomSound;
                    }

                    if (config.UseCustomSound)
                    {
                        var path = config.CustomFilePath ?? "";
                        ImGui.InputText("", ref path, 512, ImGuiInputTextFlags.ReadOnly);
                        ImGui.SameLine();
            
            
                        void UpdatePath(bool success, List<string> paths)
                        {
                            if (success && paths.Count > 0)
                            {
                                config.CustomFilePath = paths[0];
                            }
                        }
            
                        if (ImGuiComponents.IconButton(FontAwesomeIcon.Folder))
                        {
                            dialogManager.OpenFileDialog("Select the file", "Audio files{.wav,.mp3}", UpdatePath, 1,
                                                         config.CustomFilePath ??
                                                         Environment.ExpandEnvironmentVariables("%USERPROFILE%"));
                        }
            
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip("Open file browser..."); 
                        }
                    }
                    
        }
        ImGui.TreePop();
    }
    
    public override void OnClose()
    {
        dialogManager.Reset();
    }
    
    

    public void Dispose()
    {
        dialogManager.Reset();
    }
}