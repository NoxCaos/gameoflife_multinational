using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GameOfLife
{
	class Cell
	{
		public Point Position { get; private set; }
		public Rectangle Bounds { get; private set; }

		public bool IsAlive { get; private set; }
        public Color Nation { get; set; }
        public int NationNum { get; set; }

        public bool NationGenerator { get; private set; }

		public Cell(Point position, int nationNum)
		{
			Position = position;
			Bounds = new Rectangle(Position.X * Game1.CellSize, Position.Y * Game1.CellSize, Game1.CellSize, Game1.CellSize);

            Nation = Color.Black;
            NationNum = nationNum;
			IsAlive = false;
		}

		public void UpdateMouse(MouseState mouseState) {
			if (Bounds.Contains(new Point(mouseState.X, mouseState.Y))) {
				// Make cells come alive with left-click, or kill them with right-click.
				if (mouseState.LeftButton == ButtonState.Pressed)
					IsAlive = true;
				else if (mouseState.RightButton == ButtonState.Pressed)
					IsAlive = false;
			}
		}

        public void Update(Grid grid, MouseState mouseState) {
            if (NationGenerator) {
                grid.PushRandomNation(NationNum, Position);
            }
            if (mouseState.RightButton == ButtonState.Pressed) {
                if (Bounds.Contains(new Point(mouseState.X, mouseState.Y))) {
                    NationGenerator = false;
                }
            }
        }

		public void Draw(SpriteBatch spriteBatch)
		{
            if (IsAlive) spriteBatch.Draw(Game1.Pixel, Bounds, Nation);
            if (NationGenerator) spriteBatch.Draw(Game1.Pixel, Bounds, Color.Gold);

			// Don't draw anything if it's dead, since the default background color is white.
		}

        public bool IsPressed(MouseState mouseState) {
            if (Bounds.Contains(new Point(mouseState.X, mouseState.Y))) {
                if (mouseState.LeftButton == ButtonState.Pressed) return true;
            }
            return false;
        }

        public void SetNationGenerator(bool set) {
            NationGenerator = set;
        }

        public void SetAlive(bool alive) {
            this.IsAlive = alive;
        }
	}
}
