﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MisterToken {
    public class SinglePlayer {
        public SinglePlayer(SinglePlayerListener listener) {
            nextTokenReadiness = 0.0f;
            board = new Board();
            tokenGenerator = new TokenGenerator(board);
            this.listener = listener;
            Start();
        }

        public void Start() {
            nextTokenReadiness = 0.0f;
            state = State.SETTING_UP_BOARD;
        }

        public void Draw(GraphicsDevice device, SpriteBatch spriteBatch) {
            if (state == State.WON) {
                device.Clear(Color.Green);
            } else if (state == State.FAILED) {
                device.Clear(Color.Red);
            } else {
                device.Clear(Color.CornflowerBlue);
            }

            // Draw the board.
            Rectangle boardRect = new Rectangle();
            boardRect.X = Constants.BOARD_RECT_X;
            boardRect.Y = Constants.BOARD_RECT_Y;
            boardRect.Width = Constants.BOARD_RECT_WIDTH;
            boardRect.Height = Constants.BOARD_RECT_HEIGHT;
            board.DrawRect(boardRect, spriteBatch);

            // Draw the token in play.
            if (tokenGenerator.GetCurrentToken() != null) {
                tokenGenerator.GetCurrentToken().DrawRect(boardRect, spriteBatch);
            }

            // Draw the next token.
            Rectangle nextRect = new Rectangle();
            nextRect.X = (int)(nextTokenReadiness * Constants.BOARD_RECT_X + (boardRect.Width / Constants.COLUMNS) * Constants.TOKEN_START_COLUMN);
            nextRect.Y = boardRect.Y - (boardRect.Height / Constants.ROWS);
            nextRect.Width = boardRect.Width;
            nextRect.Height = boardRect.Height;
            tokenGenerator.Draw(nextRect, spriteBatch);
        }

        public void Update(GameTime gameTime) {
            switch (state) {
                case State.SETTING_UP_BOARD:
                    DoSettingUpBoard(gameTime);
                    break;
                case State.WAITING_FOR_TOKEN:
                    DoWaitingForToken(gameTime);
                    break;
                case State.MOVING_TOKEN:
                    DoMovingToken(gameTime);
                    break;
                case State.CLEARING:
                    DoClearing(gameTime);
                    break;
                case State.FALLING:
                    DoFalling(gameTime);
                    break;
                case State.FAILED:
                    DoFailed(gameTime);
                    break;
                case State.WON:
                    DoWon(gameTime);
                    break;
            }
        }

        private void DoSettingUpBoard(GameTime gameTime) {
            board.Randomize(Constants.TOP_FILLED_ROW);
            state = State.WAITING_FOR_TOKEN;
        }

        private void DoWaitingForToken(GameTime gameTime) {
            timeToNextToken -= gameTime.ElapsedGameTime.Milliseconds;
            nextTokenReadiness = 1.0f - ((float)timeToNextToken / Constants.MILLIS_PER_TOKEN);
            if (timeToNextToken <= 0) {
                tokenGenerator.LoadNextToken();
                nextTokenReadiness = 0.0f;
                Token token = tokenGenerator.GetCurrentToken();
                token.Move(0, Constants.TOKEN_START_COLUMN);
                if (!token.IsValid()) {
                    // Game over!
                    token.Commit();
                    state = State.FAILED;
                    listener.OnFailed();
                } else {
                    timeUntilNextAdvance = Constants.MILLIS_PER_ADVANCE;
                    state = State.MOVING_TOKEN;
                }
            }
        }

        private void DoMovingToken(GameTime gameTime) {
            Token currentToken = tokenGenerator.GetCurrentToken();
            if (currentToken == null) {
                throw new InvalidOperationException("Should never be in MovingTokenState with null current token.");
            }

            if (Input.IsDown(BooleanInputHook.PLAYER_ONE_TOKEN_RIGHT)) {
                if (currentToken.CanMove(0, 1)) {
                    board.ShiftRight();
                }
            }
            if (Input.IsDown(BooleanInputHook.PLAYER_ONE_TOKEN_LEFT)) {
                if (currentToken.CanMove(0, -1)) {
                    board.ShiftLeft();
                }
            }
            if (Input.IsDown(BooleanInputHook.PLAYER_ONE_ROTATE_LEFT)) {
                if (currentToken.CanRotateLeft())
                    currentToken.RotateLeft();
            }
            if (Input.IsDown(BooleanInputHook.PLAYER_ONE_ROTATE_RIGHT)) {
                if (currentToken.CanRotateRight())
                    currentToken.RotateRight();
            }
            if (Input.IsDown(BooleanInputHook.PLAYER_ONE_TOKEN_DOWN)) {
                timeUntilNextAdvance = 0;
            }
            if (Input.IsDown(BooleanInputHook.PLAYER_ONE_TOKEN_SLAM)) {
                timeUntilNextAdvance = 0;
                while (currentToken.CanMove(1, 0)) {
                    currentToken.Move(1, 0);
                }
            }

            timeUntilNextAdvance -= gameTime.ElapsedGameTime.Milliseconds;
            if (timeUntilNextAdvance <= 0) {
                timeUntilNextAdvance = Constants.MILLIS_PER_ADVANCE;
                // If there's a current token, move it down.
                if (!currentToken.CanMove(1, 0)) {
                    currentToken.Commit();
                    tokenGenerator.ClearCurrentToken();
                    timeToClear = 0;
                    state = State.CLEARING;
                } else {
                    currentToken.Move(1, 0);
                }
            }
        }

        private void DoClearing(GameTime gameTime) {
            if (timeToClear > 0) {
                timeToClear -= gameTime.ElapsedGameTime.Milliseconds;
            }
            if (timeToClear <= 0) {
                board.ClearMatches();
                bool more = board.MarkMatches();
                if (board.GetLockedCount() == 0) {
                    state = State.WON;
                    listener.OnWon();
                } else if (more) {
                    timeToClear = Constants.MILLIS_PER_CLEAR;
                    listener.OnClear();
                } else {
                    timeToNextFall = 0;
                    anythingFell = false;
                    state = State.FALLING;
                }
            }
        }

        private void DoFalling(GameTime gameTime) {
            if (timeToNextFall > 0) {
                timeToNextFall -= gameTime.ElapsedGameTime.Milliseconds;
            }
            if (timeToNextFall <= 0) {
                if (board.ApplyGravity()) {
                    anythingFell = true;
                    timeToNextFall = Constants.MILLIS_PER_FALL;
                } else {
                    if (anythingFell) {
                        timeToClear = 0;
                        state = State.CLEARING;
                    } else {
                        timeToNextToken = Constants.MILLIS_PER_TOKEN;
                        state = State.WAITING_FOR_TOKEN;
                    }
                }
            }
        }

        private void DoFailed(GameTime gameTime) {
            if (Input.IsDown(BooleanInputHook.PLAYER_ONE_START)) {
                listener.OnFinished();
            }
        }

        private void DoWon(GameTime gameTime) {
            if (Input.IsDown(BooleanInputHook.PLAYER_ONE_START)) {
                listener.OnFinished();
            }
        }

        // Game state.
        private enum State {
            SETTING_UP_BOARD,
            WAITING_FOR_TOKEN,
            MOVING_TOKEN,
            CLEARING,
            FALLING,
            FAILED,
            WON,
        }
        private State state;
        private SinglePlayerListener listener;

        // Waiting for token.
        private int timeToNextToken;

        // Moving token.
        private int timeUntilNextAdvance;

        // Clearing.
        private int timeToClear;

        // Falling.
        private bool anythingFell;
        private int timeToNextFall;

        // Internal state.
        private Board board;
        private TokenGenerator tokenGenerator;
        private float nextTokenReadiness;
    }
}