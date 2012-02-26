using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace MisterToken {
    public class MisterTokenGame : Microsoft.Xna.Framework.Game, GameListener, TitleMenuListener {
        public MisterTokenGame() {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            graphics.PreferredBackBufferHeight = 720;
            graphics.PreferredBackBufferWidth = 1280;
            graphics.ApplyChanges();

            titleMenu = new TitleMenu(this);
        }

        protected override void Initialize() {
            base.Initialize();
        }

        protected override void LoadContent() {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            Sprites.LoadContent(Content, GraphicsDevice);
            Sound.LoadContent(Content);
        }

        protected override void UnloadContent() {
        }

        protected override void Update(GameTime gameTime) {
            Input.Update(gameTime);

            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                this.Exit();

            switch (state) {
                case State.WAITING_TO_PLAY:
                    titleMenu.Update();
                    break;
                case State.PLAYING:
                    model.Update(gameTime);
                    break;
            }
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime) {
            switch (state) {
                case State.WAITING_TO_PLAY:
                    GraphicsDevice.Clear(Color.Black);
                    spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                    titleMenu.Draw(spriteBatch);
                    spriteBatch.End();
                    break;
                case State.PLAYING:
                    GraphicsDevice.Clear(Color.Black);
                    spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                    Sprites.DrawLayer(SpriteHook.BACKGROUND_LAYER, spriteBatch);
                    model.Draw(GraphicsDevice, spriteBatch);
                    spriteBatch.End();
                    break;
            }
            base.Draw(gameTime);
        }

        public void OnClear(PlayerIndex player) {
        }

        public void OnDump(PlayerIndex player, List<Cell.Color> colors) {
        }

        public void OnWon(PlayerIndex player) {
        }

        public void OnFailed(PlayerIndex player) {
        }

        public void OnFinished(PlayerIndex player) {
            state = State.WAITING_TO_PLAY;
        }

        public void OnStartSinglePlayer() {
            model = new SinglePlayer(PlayerIndex.One, this);
            state = State.PLAYING;
        }

        public void OnStartMultiPlayer() {
            model = new MultiPlayer(this);
            state = State.PLAYING;
        }

        // Game state.
        private enum State {
            WAITING_TO_PLAY,
            PLAYING,
        }
        private State state;

        // Data model.
        TitleMenu titleMenu;
        private Game model;

        // UI stuff.
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
    }
}
