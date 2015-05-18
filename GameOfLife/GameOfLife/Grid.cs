using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Threading.Tasks;

namespace GameOfLife {
	class Grid {
		public Point Size { get; private set; }

		private Cell[][,] cells;
		private bool[][,] nextCellStates;
        private int[][,] cellNeighbours;

		private TimeSpan updateTimer;

        private KeyboardState keyboardState, keyboardLastState;

		public Grid() {
			Size = new Point(Game1.CellsX, Game1.CellsY);

			cells = new Cell[Game1.nations][,];
            nextCellStates = new bool[Game1.nations][,];
            cellNeighbours = new Int32[Game1.nations][,];

            Random rand = new Random();
            for (int n = 0; n < Game1.nations; n++) {
                cells[n] = new Cell[Size.X, Size.Y];
                nextCellStates[n] = new bool[Size.X, Size.Y];
                cellNeighbours[n] = new Int32[Size.X, Size.Y];
                Color col = Color.FromNonPremultiplied(rand.Next(0, 200), rand.Next(0, 200), rand.Next(0, 200), 255);
                for (int i = 0; i < Size.X; i++) {
                    for (int j = 0; j < Size.Y; j++) {
                        cells[n][i, j] = new Cell(new Point(i, j), n);
                        cells[n][i, j].Nation = col;
                        nextCellStates[n][i, j] = false;
                    }
                }
            }

            keyboardLastState = keyboardState = Keyboard.GetState();
			updateTimer = TimeSpan.Zero;
		}

        public void PushRandom() {
            Random rand = new Random();
            foreach (Cell[,] cell in cells) {
                Point p = new Point(rand.Next(0, Size.X), rand.Next(0, Size.Y));
                for (int i = 0; i < Size.X; i++) {
                    for (int j = 0; j < Size.Y; j++) {
                        int randLimit = (int)(Math.Abs(p.X - i) + Math.Abs(p.Y - j)) / 2;
                        if (rand.Next(0, randLimit) == 0) {
                            cell[i, j].SetAlive(true);
                        }
                    }
                }
            }
        }

        public void PushRandomNation(int nat, Point p) {
            Random rand = new Random();
            for (int i = 0; i < Size.X; i++) {
                for (int j = 0; j < Size.Y; j++) {
                    int randLimit = (int)(Math.Abs(p.X - i) + Math.Abs(p.Y - j)) * 2;
                    if (rand.Next(0, randLimit) == 0) {
                        cells[nat][i, j].SetAlive(true);
                    }
                }
             }
        }

		public void Clear() {
            foreach (bool[,] cell in nextCellStates)
			    for (int i = 0; i < Size.X; i++)
				    for (int j = 0; j < Size.Y; j++)
					    cell[i, j] = false;

			SetNextState();
		}

		public void Update(GameTime gameTime) {
			MouseState mouseState = Mouse.GetState();
            keyboardState = Keyboard.GetState();

            int updateMatr = -1;
            if (keyboardState.IsKeyDown(Keys.D1)) updateMatr = 0;
            else if (keyboardState.IsKeyDown(Keys.D2)) updateMatr = 1;
            else if (keyboardState.IsKeyDown(Keys.D3)) updateMatr = 2;
            else if (keyboardState.IsKeyDown(Keys.D4)) updateMatr = 3;
            else if (keyboardState.IsKeyDown(Keys.D5)) updateMatr = 4;
            else if (keyboardState.IsKeyDown(Keys.D6)) updateMatr = 5;
            else if (keyboardState.IsKeyDown(Keys.D7)) updateMatr = 6;
            else if (keyboardState.IsKeyDown(Keys.D8)) updateMatr = 7;
            else if (keyboardState.IsKeyDown(Keys.D9)) updateMatr = 8;
            else if (keyboardState.IsKeyDown(Keys.D0)) updateMatr = 9;

			updateTimer += gameTime.ElapsedGameTime;

			if (updateTimer.TotalMilliseconds > 1000f / Game1.UPS) {
				updateTimer = TimeSpan.Zero;

                if (updateMatr > -1 && updateMatr < Game1.nations) {
                    if (keyboardState.IsKeyDown(Keys.LeftShift) && keyboardLastState.IsKeyUp(Keys.LeftShift)) {
                        foreach (Cell cell in cells[updateMatr]) {
                            if (cell.IsPressed(mouseState)) {
                                cell.SetNationGenerator(!cell.NationGenerator);
                                break;
                            }
                        }
                    }
                    else {
                        foreach (Cell cell in cells[updateMatr]) {
                            if (cell.IsPressed(mouseState)) {
                                PushRandomNation(updateMatr, cell.Position);
                                break;
                            }
                        }
                    }
                }

                keyboardLastState = keyboardState;

                if (Game1.Paused)
                    return;

                foreach (Cell[,] cell in cells)
                    foreach (Cell c in cell)
                        c.Update(this, mouseState);

				// Loop through every cell on the grid.
                Parallel.For(0, Game1.nations, n =>
                {
                    Parallel.For(0, Size.X, i =>
                    {
                        for (int j = 0; j < Size.Y; j++) {
                            // Check the cell's current state, count its living neighbors, and apply the rules to set its next state.
                            bool living = cells[n][i, j].IsAlive;
                            int count = GetLivingNeighbors(i, j, n);

                            bool keep = true;
                            for (int k = 0; k < Game1.nations; k++) {
                                if (k != n && cells[k][i, j].IsAlive) {
                                    if (cellNeighbours[k][i, j] > count) {
                                        nextCellStates[n][i, j] = false;
                                        cells[n][i, j].SetNationGenerator(false);
                                        keep = false;
                                        break;
                                    }
                                    else if (cellNeighbours[k][i, j] <= count) {
                                        nextCellStates[k][i, j] = false;
                                    }
                                }
                            }

                            if (!keep) continue;
                            bool result = false;

                            if (living && count < 2)
                                result = false;
                            if (living && (count == 2 || count == 3))
                                result = true;
                            if (living && count > 3)
                                result = false;
                            if (!living && count == 3)
                                result = true;

                            nextCellStates[n][i, j] = result;
                        }
                    });
                });

				SetNextState();
			}
		}

		public int GetLivingNeighbors(int x, int y, int nat) {
			int count = 0;

			// Check cell on the right.
			if (x != Size.X - 1)
				if (cells[nat][x + 1, y].IsAlive)
					count++;

			// Check cell on the bottomw right.
			if (x != Size.X - 1 && y != Size.Y - 1)
				if (cells[nat][x + 1, y + 1].IsAlive)
					count++;

			// Check cell on the bottom.
			if (y != Size.Y - 1)
				if (cells[nat][x, y + 1].IsAlive)
					count++;

			// Check cell on the bottom left.
			if (x != 0 && y != Size.Y - 1)
				if (cells[nat][x - 1, y + 1].IsAlive)
					count++;

			// Check cell on the left.
			if (x != 0)
				if (cells[nat][x - 1, y].IsAlive)
					count++;

			// Check cell on the top left.
			if (x != 0 && y != 0)
				if (cells[nat][x - 1, y - 1].IsAlive)
					count++;

			// Check cell on the top.
			if (y != 0)
				if (cells[nat][x, y - 1].IsAlive)
					count++;

			// Check cell on the top right.
			if (x != Size.X - 1 && y != 0)
				if (cells[nat][x + 1, y - 1].IsAlive)
					count++;

            cellNeighbours[nat][x, y] = count;
			return count;
		}

		public void SetNextState() {
            for (int n = 0; n < Game1.nations; n++)
			    for (int i = 0; i < Size.X; i++)
				    for (int j = 0; j < Size.Y; j++)
					    cells[n][i, j].SetAlive(nextCellStates[n][i, j]);
		}

		public void Draw(SpriteBatch spriteBatch) {
            foreach (Cell[,] cellMatr in cells)
			    foreach (Cell cell in cellMatr)
				    cell.Draw(spriteBatch);

			// Draw vertical gridlines.
			for (int i = 0; i < Size.X; i++)
				spriteBatch.Draw(Game1.Pixel, new Rectangle(i * Game1.CellSize - 1, 0, 1, Size.Y * Game1.CellSize), Color.DarkGray);

			// Draw horizontal gridlines.
			for (int j = 0; j < Size.Y; j++)
				spriteBatch.Draw(Game1.Pixel, new Rectangle(0, j * Game1.CellSize - 1, Size.X * Game1.CellSize, 1), Color.DarkGray);
		}
	}
}
