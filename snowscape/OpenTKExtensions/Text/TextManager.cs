﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using OpenTK;
using OpenTKExtensions.Framework;

namespace OpenTKExtensions.Text
{
    public class TextManager : GameComponentBase, IRenderable, IResizeable
    {
        public string Name { get; set; }
        public Font Font { get; set; }
        public bool NeedsRefresh { get; private set; }
        public bool Visible { get; set; }
        public int DrawOrder { get; set; }
        public Matrix4 Projection { get; set; }
        public Matrix4 Modelview { get; set; }
        public bool AutoTransform { get; set; }

        Dictionary<string, TextBlock> blocks = new Dictionary<string, TextBlock>();
        public Dictionary<string, TextBlock> Blocks { get { return blocks; } }

        public TextManager(string name, Font font)
        {
            this.Name = name;
            this.Font = font;
            this.NeedsRefresh = false;
            this.Visible = true;
            this.DrawOrder = int.MaxValue;
            this.AutoTransform = false;
            this.Projection = Matrix4.Identity;
            this.Modelview = Matrix4.Identity;
        }

        public TextManager()
            : this("unnamed", null)
        {

        }

        public void Clear()
        {
            this.Blocks.Clear();
            this.NeedsRefresh = true;
        }

        public bool Add(TextBlock b)
        {
            if (!Blocks.ContainsKey(b.Name))
            {
                log.Trace("TextManager.Add ({0}): Adding \"{1}\"", this.Name, b.Text);
                Blocks.Add(b.Name, b);
                this.NeedsRefresh = true;
                return true;
            }
            return false;
        }

        public void AddOrUpdate(TextBlock b)
        {
            if (!Add(b))
            {
                Blocks[b.Name] = b;
                this.NeedsRefresh = true;
            }
        }

        public bool Remove(string blockName)
        {
            if (this.Blocks.ContainsKey(blockName))
            {
                this.Blocks.Remove(blockName);
                this.NeedsRefresh = true;
                return true;
            }
            return false;
        }
        public bool RemoveAllByPrefix(string blockNamePrefix)
        {
            bool hit = false;

            foreach (var blockToRemove in this.Blocks.Keys.Where(n => n.StartsWith(blockNamePrefix)).ToList())
            {
                this.Blocks.Remove(blockToRemove);
                hit = true;
            }

            if (hit)
            {
                this.NeedsRefresh = true;
                return true;
            }
            return false;
        }


        public void Refresh()
        {
            log.Trace("TextManager.Refresh ({0}): Refreshing {1} blocks...", this.Name, this.Blocks.Count);

            if (this.Font == null)
            {
                log.Warn("TextManager.Refresh ({0}): Font not specified so bailing out.", this.Name);
                return;
            }

            if (!this.Font.IsLoaded)
            {
                log.Warn("TextManager.Refresh ({0}): Font not loaded so bailing out.", this.Name);
                return;
            }

            // refresh character arrays
            this.Font.Clear();

            foreach (var b in this.Blocks.Values)
            {
                this.Font.AddString(b.Text, b.Position, b.Size, b.Colour);
            }

            this.Font.Refresh();
            this.NeedsRefresh = false;
        }

        public void Render(IFrameRenderData frameData)
        {
            this.Render();
        }
        public void Render()
        {
            if (this.Font == null)
            {
                log.Warn("TextManager.Render ({0}): Font not specified so bailing out.", this.Name);
                return;
            }
            if (!this.Font.IsLoaded)
            {
                log.Warn("TextManager.Refresh ({0}): Font not loaded so bailing out.", this.Name);
                return;
            }

            if (this.NeedsRefresh)
            {
                this.Refresh();
            }

            this.Font.Render(this.Projection, this.Modelview);
        }


        public void Resize(int width, int height)
        {
            if (height > 0)
            {
                this.Projection = Matrix4.CreateOrthographicOffCenter(0.0f, (float)width / (float)height, 1.0f, 0.0f, 0.001f, 10.0f);
                this.Modelview = Matrix4.Identity * Matrix4.CreateTranslation(0.0f, 0.0f, -1.0f);
            }
            else
            {
                this.Projection = this.Modelview = Matrix4.Identity;
            }
            
        }
    }
}
