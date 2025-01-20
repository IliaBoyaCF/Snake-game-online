using Serilog;
using SnakeOnline.Game.States;
using Snakes;
using System.Net;

namespace Network.Node;

public class Master : Node
{

    public static Master InitNew(NodeContext context)
    {
        Master master = new Master(context);
        IPlayerState? player = master._context.SynchronizedOnGame(_ => master._context.Game.GetState().GetIPlayerState().Find(p => p.GetId() == master._context.MyId));
        if (player == null)
        {
            throw new InvalidOperationException("Can't create master node without created player in the game.");
        }
        master._context.SynchronizedOnNodes(_ =>
        {
            master._context.Nodes.Add(new GamePlayer()
            {
                Role = NodeRole.Master,
                Id = master._context.MyId,
                Score = player.GetScore(),
                Name = player.GetName(),
                Type = player.GetPlayerType() == IPlayerState.Type.HUMAN ? PlayerType.Human : PlayerType.Robot,
            });
        }
        );
        master._context.NodeRadar.Start();
        return master;
    }

    public static Master InitFromDeputyPromotion(NodeContext context, int previousMasterId)
    {
        Log.Debug("Promoted to master from deputy.");

        Master master = new Master(context);

        master.OnNodeDisconnect(previousMasterId);

        context.SynchronizedOnNodes(_ =>
        {
            GamePlayer me = context.Nodes.FindById(master._context.MyId);
            context.Nodes.Remove(me);
            context.Nodes.Add(new GamePlayer(me)
            {
                Role = NodeRole.Master,
            });
        });
        master.NotifyAllThatImMaster();
        master.ChooseNewDeputy();
        Log.Debug("Promotion completed starting duty.");
        return master;
    }

    private void NotifyAllThatImMaster()
    {
        Log.Debug("Sending notifications that I've become MASTER");
        _context.SynchronizedOnNodes(_ =>
        {
            foreach (GamePlayer gamePlayer in _context.Nodes)
            {
                if (gamePlayer.Id == _context.MyId)
                {
                    continue;
                }
                _context.MessageDeliveryController.Deliver(new GameMessage()
                {
                    MsgSeq = _context.MessageSeq,
                    ReceiverId = gamePlayer.Id,
                    SenderId = _context.MyId,
                    RoleChange = new GameMessage.Types.RoleChangeMsg()
                    {
                        SenderRole = NodeRole.Master,
                    }
                }, new IPEndPoint(IPAddress.Parse(gamePlayer.IpAddress), gamePlayer.Port));
            }
        });
    }

    public Master(NodeContext context) : base(context)
    {
        Log.Debug("Become master.");
    }

    public override void OnDiscoverReceived(IPEndPoint sender, GameMessage message)
    {
        GameMessage.Types.AnnouncementMsg gameAnnouncement = GetAnnouncement();
        GameMessage response = new GameMessage()
        {
            MsgSeq = _context.MessageSeq,
            SenderId = _context.MyId,
            Announcement = gameAnnouncement,
        };
        _context.MessageDeliveryController.SendUnicastOnce(response, sender);
    }

    private GameMessage.Types.AnnouncementMsg GetAnnouncement()
    {
        GamePlayers gamePlayers = GetGamePlayers();

        foreach (GamePlayer gamePlayer in gamePlayers.Players)
        {
            if (gamePlayer.Role == NodeRole.Master)
            {
                Log.Debug($"Master node: {gamePlayer.Id}");
            }
        };

        GameConfig gameConfig = GetGameConfig();
        bool canJoin = false;
        string gameName = string.Empty;
        _context.SynchronizedOnGame((context) =>
                                    {
                                        canJoin = _context.Game.CanAddNewPlayer;
                                        gameName = _context.Game.Name;
                                    }
        );
        GameMessage.Types.AnnouncementMsg result = new GameMessage.Types.AnnouncementMsg();
        result.Games.Add(new GameAnnouncement()
        {
            GameName = gameName,
            CanJoin = canJoin,
            Config = gameConfig,
            Players = gamePlayers,
        });
        return result;
    }

    public override void OnNodeDisconnect(int nodeId)
    {
        GamePlayer node = null;
        _context.SynchronizedOnNodes(_ =>
        {
            node = _context.Nodes.Find((n) => n.Id == nodeId);
            _context.MessageDeliveryController.RemoveAllDeliveries(new IPEndPoint(IPAddress.Parse(node.IpAddress), node.Port));
            _context.Nodes.Remove(node);
        });
        _context.SynchronizedOnGame(_ =>
        {
            if (_context.Game.PlayerExists(nodeId))
            {
                _context.Game.KillPlayer(nodeId);
            }
        });
        if (node.Role == NodeRole.Deputy)
        {
            ChooseNewDeputy();
        }
    }

    private void ChooseNewDeputy()
    {
        Log.Debug("Choosing new deputy.");
        GamePlayer newDeputy = null;
        _context.SynchronizedOnNodes(_ =>
        {
            if (_context.Nodes.Count() < 2) { return; }
            newDeputy = _context.Nodes.Find((n) => n.Role == NodeRole.Normal);
            _context.Nodes.Remove(newDeputy);
            _context.Nodes.Add(new GamePlayer(newDeputy)
            {
                Role = NodeRole.Deputy,
            });
        });
        if (newDeputy == null)
        {
            Log.Debug("Couldn't choose deputy.");
            return;
        }
        _context.MessageDeliveryController.Deliver(new GameMessage()
        {
            MsgSeq = _context.MessageSeq,
            SenderId = _context.MyId,
            ReceiverId = newDeputy.Id,
            RoleChange = new GameMessage.Types.RoleChangeMsg()
            {
                ReceiverRole = NodeRole.Deputy,
            },
        }, GetIpEndPoint(newDeputy));
        Log.Debug($"{newDeputy.Id} chosen as a new deputy.");
    }

    private GamePlayers GetGamePlayers()
    {
        GamePlayers gamePlayers = new GamePlayers();
        _context.SynchronizedOnNodes(_ => gamePlayers.Players.AddRange(_context.Nodes));
        return gamePlayers;
    }

    private GameConfig GetGameConfig()
    {
        GameConfig gameConfig = null;
        _context.SynchronizedOnGame((context) =>
        {
            gameConfig = new GameConfig()
            {
                Width = _context.Game.FieldWidth,
                Height = _context.Game.FieldHeight,
                StateDelayMs = _context.Game.StateDelayMs,
                FoodStatic = _context.Game.FoodStatic,
            };
        });
        return gameConfig;
    }

    // ! Some problems when AckMsg is lost.
    public override void OnJoinReceived(IPEndPoint sender, GameMessage message)
    {
        if (message.MsgSeq != 0)
        {
            SendAckTo(sender, message, _context.SynchronizedOnNodes(_ => _context.Nodes.Find((n) => message.Join.PlayerName == n.Name).Id));
        }
        if (_context.SynchronizedOnNodes(_ => _context.Nodes.Contains((n) => message.Join.PlayerName == n.Name)))
        {
            _context.MessageDeliveryController.Deliver(new GameMessage()
            {
                MsgSeq = _context.MessageSeq,
                SenderId = _context.MyId,
                Error = new GameMessage.Types.ErrorMsg()
                {
                    ErrorMessage = $"Name: {message.Join.PlayerName} is already taken. Try other.",
                }
            }, sender);
            return;
        }
        switch (message.Join.RequestedRole)
        {
            case NodeRole.Viewer:
                {
                    int newPlayerId = 0;
                    _context.SynchronizedOnGame((_) =>
                    {
                        newPlayerId = _context.Game.GenerateNewPlayerId();
                    });
                    SendAckTo(sender, message, newPlayerId);
                    _context.SynchronizedOnNodes(_ => _context.Nodes.Add(new GamePlayer()
                    {
                        Id = newPlayerId,
                        Name = message.Join.PlayerName,
                        Score = 0,
                        IpAddress = sender.Address.ToString(),
                        Port = sender.Port,
                        Role = NodeRole.Viewer,
                        Type = PlayerType.Human,
                    }));
                    return;
                }
            case NodeRole.Normal:
                {
                    Log.Debug($"Got join message from {sender}. Trying to connect as Normal.");
                    bool canAdd = false;
                    _context.SynchronizedOnGame((_) =>
                    {
                        canAdd = _context.Game.CanAddNewPlayer;
                    });
                    if (!canAdd)
                    {
                        _context.MessageDeliveryController.Deliver(new GameMessage()
                        {
                            MsgSeq = message.MsgSeq,
                            SenderId = _context.MyId,
                            Error = new GameMessage.Types.ErrorMsg()
                            {
                                ErrorMessage = "No available space on the field.",
                            },
                        }, sender);
                        return;
                    }
                    int newPlayerId = 0;
                    _context.SynchronizedOnGame(_ =>
                    {
                        newPlayerId = _context.Game.NewPlayer(message.Join.PlayerName);
                    });
                    SendAckTo(sender, message, newPlayerId);
                    _context.SynchronizedOnNodes(_ => _context.Nodes.Add(new GamePlayer()
                    {
                        Id = newPlayerId,
                        Name = message.Join.PlayerName,
                        Score = 0,
                        IpAddress = sender.Address.ToString(),
                        Port = sender.Port,
                        Role = NodeRole.Normal,
                        Type = PlayerType.Human,
                    }));
                    return;
                }
            default:
                SendWarningToCheater(sender);
                break;
        }
    }

    public override void OnRoleChangeReceived(IPEndPoint sender, GameMessage message)
    {
        if (AnswerIsDelivering(sender, message))
        {
            return;
        }
        if (message.RoleChange.HasReceiverRole || message.RoleChange.SenderRole != NodeRole.Viewer)
        {
            SendWarningToCheater(sender);
            return;
        }
        GamePlayer changedRoleOld = null;
        _context.SynchronizedOnNodes(_ =>
        {
            if (!_context.Nodes.Contains((n) => n.Id == message.SenderId))
            {
                return;
            }

            //_context.MessageDeliveryController.SendUnicastOnce(message, sender);

            SendAckTo(sender, message, message.SenderId);

            //_context.NodeRadar.GotMessageFrom(message.SenderId);
            changedRoleOld = _context.Nodes.Find((n) => message.SenderId == n.Id);
            _context.Nodes.Remove(changedRoleOld);
            _context.Nodes.Add(new GamePlayer(changedRoleOld)
            {
                Role = NodeRole.Viewer,
            });

        });
        if (changedRoleOld != null)
        {
            _context.SynchronizedOnGame(_ =>
            {
                _context.Game.KillPlayer(changedRoleOld.Id);
            });
        }
    }

    public override void OnStateReceived(IPEndPoint sender, GameMessage message)
    {
        return;
    }

    public override void OnSteerReceived(IPEndPoint sender, GameMessage message)
    {
        if (_context.MessageDeliveryController.GetPending(sender).Find((m) => m.MsgSeq == message.MsgSeq) != null)
        {
            return;
        }
        if (!message.HasSenderId || !message.HasReceiverId || message.ReceiverId != _context.MyId)
        {
            return;
        }
        if (!_context.SynchronizedOnNodes(_ => _context.Nodes.Contains((n) => n.Id == message.SenderId)))
        {
            return;
        }

        SendAckTo(sender, message, message.SenderId);

        _context.SynchronizedOnGame(_ =>
        {
            _context.Game.ChangePlayersSnakeDirection(message.SenderId, GetDirection(message.Steer.Direction));
        });
    }

    public void Announce()
    {
        _context.MessageDeliveryController.SendMulticastOnce(new GameMessage()
        {
            MsgSeq = _context.MessageSeq,
            SenderId = _context.MyId,
            Announcement = GetAnnouncement(),
        });
    }

    public void Routine()
    {
        Log.Debug("Master called routine.");
        IGameState gameState = null;
        _context.SynchronizedOnGame(_ =>
        {
            _context.Game.Update();
            gameState = _context.Game.GetState();
        });

        Log.Debug("Master updated game state and got it.");

        List<int> deadPlayers = GetDeadPlayers(gameState);
        Log.Debug("Master got dead players: {0}", deadPlayers);
        foreach (int deadPlayer in deadPlayers)
        {
            OnPlayerDeath(deadPlayer);
        }
        Snakes.GameState sendingState = GetGameState(gameState);
        _context.SynchronizedOnNodes(_ =>
        {
            Log.Debug("Sending game state to connected nodes.");
            foreach (GamePlayer gamePlayer in _context.Nodes)
            {
                Log.Debug($"Willing to send game state to {gamePlayer.Id}");
                if (gamePlayer.Id == _context.MyId)
                {
                    continue;
                }
                SendGameStateTo(gamePlayer, new GameMessage.Types.StateMsg()
                {
                    State = sendingState
                });
            }
            Log.Debug("Checking is there deputy.");
            if (_context.Nodes.GetDeputyNode() == null)
            {
                //if (_context.MessageDeliveryController.IsPending((m) => m.TypeCase == GameMessage.TypeOneofCase.RoleChange && m.RoleChange.ReceiverRole == NodeRole.Deputy))
                //{
                //    return;
                //}
                ChooseNewDeputy();
            }
        });

        Log.Debug("Master completed sending state messages.");

        _context.OnGameStateUpdate(gameState);
        Log.Debug("Master routine ended.");
    }

    private void SendGameStateTo(GamePlayer gamePlayer, GameMessage.Types.StateMsg sendingState)
    {
        _context.MessageDeliveryController.Deliver(new GameMessage()
        {
            MsgSeq = _context.MessageSeq,
            SenderId = _context.MyId,
            ReceiverId = gamePlayer.Id,
            State = sendingState,
        }, new IPEndPoint(IPAddress.Parse(gamePlayer.IpAddress), gamePlayer.Port));
    }

    private Snakes.GameState GetGameState(IGameState gameState)
    {
        Log.Debug("Parsing GameState to protobuf GameStateMessage");
        Snakes.GameState state = new Snakes.GameState()
        {
            Players = new GamePlayers(),
        };
        foreach (IFoodState foodState in gameState.GetFieldState().GetFoodState())
        {
            state.Foods.Add(new Snakes.GameState.Types.Coord()
            {
                X = foodState.GetCoordinates().GetX(),
                Y = foodState.GetCoordinates().GetY(),
            });
        }

        _context.SynchronizedOnNodes(_ =>
        {
            foreach (IPlayerState playerState in gameState.GetIPlayerState())
            {
                Log.Debug($"Getting state of {playerState.GetId()}");
                if (!_context.Nodes.Contains(playerState.GetId()))
                {
                    continue;
                }
                GamePlayer gamePlayer = _context.Nodes.FindById(playerState.GetId());
                _context.Nodes.Remove(gamePlayer);
                gamePlayer = new GamePlayer(gamePlayer)
                {
                    Score = playerState.GetScore(),
                };
                _context.Nodes.Add(gamePlayer);
                state.Players.Players.Add(gamePlayer);
            }
        });

        foreach (ISnakeState snakeState in gameState.GetFieldState().GetSnakesState())
        {
            state.Snakes.Add(GetSnake(snakeState));
        }

        Log.Debug("GameState parsing success.");

        return state;
    }

    private Snakes.GameState.Types.Snake GetSnake(ISnakeState snakeState)
    {
        Snakes.GameState.Types.Snake snake = new Snakes.GameState.Types.Snake()
        {
            State = snakeState.GetStatus() == ISnakeState.SnakeStatus.ALIVE ? Snakes.GameState.Types.Snake.Types.SnakeState.Alive : Snakes.GameState.Types.Snake.Types.SnakeState.Zombie,
            PlayerId = snakeState.GetPlayerId(),
            HeadDirection = ToProtobufDirection(snakeState.GetDirection()),
        };
        foreach (ISnakeState.IBody body in snakeState.GetBody())
        {
            snake.Points.Add(new Snakes.GameState.Types.Coord()
            {
                X = body.GetCoordinates().GetX(),
                Y = body.GetCoordinates().GetY(),
            });
        }
        return snake;
    }

    private void OnPlayerDeath(int deadPlayer)
    {
        GamePlayer old = null;
        _context.SynchronizedOnNodes(_ =>
        {
            old = _context.Nodes.Find((n) => n.Id == deadPlayer);
            _context.Nodes.Remove(old);
            _context.Nodes.Add(new GamePlayer(old)
            {
                Role = NodeRole.Viewer,
            });
        });
        if (deadPlayer == _context.MyId)
        {
            return;
        }
        _context.MessageDeliveryController.Deliver(new GameMessage()
        {
            MsgSeq = _context.MessageSeq,
            SenderId = _context.MyId,
            ReceiverId = deadPlayer,
            RoleChange = new GameMessage.Types.RoleChangeMsg()
            {
                ReceiverRole = NodeRole.Viewer,
            }
        }, new IPEndPoint(IPAddress.Parse(old.IpAddress), old.Port));
    }

    private List<int> GetDeadPlayers(IGameState gameState)
    {
        List<int> deadPlayers = _context.SynchronizedOnNodes(_ => _context.Nodes.FindPlayers());
        foreach (IPlayerState player in gameState.GetIPlayerState())
        {
            deadPlayers.Remove(player.GetId());
        }
        return deadPlayers;
    }

    public override void ChangePlayerDirection(int id, ISnakeState.Direction direction)
    {
        if (!_context.SynchronizedOnNodes(_ => _context.Nodes.Contains(n => n.Id == id)))
        {
            throw new ArgumentException("No player with such id.");
        }
        if (_context.SynchronizedOnNodes(_ => _context.Nodes.FindById(id).Role == NodeRole.Viewer))
        {
            throw new ArgumentException("Viewer doesn't have snake to change it's direction.");
        }
        _context.SynchronizedOnGame(_ =>
        {
            _context.Game.ChangePlayersSnakeDirection(id, direction);
        });
    }
}
