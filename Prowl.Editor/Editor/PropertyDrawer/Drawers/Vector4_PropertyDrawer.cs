﻿// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Prowl.Runtime;
using Prowl.Runtime.GUI;

namespace Prowl.Editor.PropertyDrawers;

[Drawer(typeof(Vector4))]
public class Vector4_PropertyDrawer : PropertyDrawer
{
    public override double MinWidth => 175;
    public override bool OnValueGUI(Gui gui, string ID, Type targetType, ref object? value, List<Attribute>? attributes = null)
    {
        gui.CurrentNode.Layout(LayoutType.Row).ScaleChildren();

        Vector4 val = (Vector4)value;
        bool changed = gui.InputDouble(ID + "X", ref val.x, 0, 0, 0, Size.Percentage(1), EditorGUI.VectorXStyle);
        changed |= gui.InputDouble(ID + "Y", ref val.y, 0, 0, 0, Size.Percentage(1), EditorGUI.VectorYStyle);
        changed |= gui.InputDouble(ID + "Z", ref val.z, 0, 0, 0, Size.Percentage(1), EditorGUI.VectorZStyle);
        changed |= gui.InputDouble(ID + "W", ref val.w, 0, 0, 0, Size.Percentage(1), EditorGUI.InputFieldStyle);
        value = val;
        return changed;
    }
}
