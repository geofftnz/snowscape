using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;
using OpenTK;

namespace OpenTKExtensions
{
    public class TextManager
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        public string Name { get; set; }
        public Font Font { get; set; }
        public bool NeedsRefresh { get; private set; }

        Dictionary<string, TextBlock> blocks = new Dictionary<string, TextBlock>();
        public Dictionary<string, TextBlock> Blocks { get { return blocks; } }

        public TextManager(string name, Font font)
        {
            this.Name = name;
            this.Font = font;
            this.NeedsRefresh = false;
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
                log.Info("TextManager.Add ({0}): Adding \"{1}\"", this.Name,b.Text);
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


        public void Refresh()
        {
            log.Info("TextManager.Refresh ({0}): Refreshing {1} blocks...", this.Name, this.Blocks.Count);

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

        public void Render(Matrix4 projection, Matrix4 modelview)
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

            this.Font.Render(projection, modelview);

        }



    }
}
