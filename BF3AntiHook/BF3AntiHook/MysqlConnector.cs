// Decompiled with JetBrains decompiler
// Type: BF3AntiHook.BF3AntiHook.MysqlConnector
// Assembly: BF3AntiHook, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: EEB90F25-279F-4551-9815-4FB977A6FF28
// Assembly location: C:\Users\Ernestico\Desktop\BF3AntiHook.exe

using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace BF3AntiHook.BF3AntiHook
{
  internal class MysqlConnector
  {
    private MySqlConnectionStringBuilder builder;
    private MySqlConnection conection;

    public MysqlConnector(string ip, string user, string password, int port, string databasename) => this.builder = new MySqlConnectionStringBuilder()
    {
      Server = ip,
      Database = databasename,
      UserID = user,
      Password = password
    };

    public async Task<bool> Connect()
    {
      this.conection = new MySqlConnection(((DbConnectionStringBuilder) this.builder).ConnectionString);
      ((DbConnection) this.conection).Open();
      return ((DbConnection) this.conection).State == ConnectionState.Open;
    }

    public List<User> GetUsers()
    {
      List<User> userList = new List<User>();
      using (MySqlCommand command = this.conection.CreateCommand())
      {
        ((DbCommand) command).CommandText = "SELECT user_id, username, password, AuthCode, devTeam, login_ip FROM a_emu_playerinfo;";
        using (MySqlDataReader mySqlDataReader = command.ExecuteReader())
        {
          while (((DbDataReader) mySqlDataReader).Read())
            userList.Add(new User()
            {
              AutToken = mySqlDataReader.IsDBNull(mySqlDataReader.GetOrdinal("AuthCode")) ? "" : mySqlDataReader.GetString("AuthCode"),
              Password = mySqlDataReader.GetString("password"),
              Username = mySqlDataReader.GetString("username"),
              userid = mySqlDataReader.GetInt32("user_id").ToString(),
              Role = mySqlDataReader.GetInt32("devTeam") != 0 ? "admin" : "player",
              IP = mySqlDataReader.IsDBNull(mySqlDataReader.GetOrdinal("login_ip")) ? "" : mySqlDataReader.GetString("login_ip")
            });
        }
      }
      return userList;
    }

    public List<Servers> GetServers()
    {
      List<Servers> serversList = new List<Servers>();
      using (MySqlCommand command = this.conection.CreateCommand())
      {
        ((DbCommand) command).CommandText = "SELECT * FROM a_bf_gameservers;";
        using (MySqlDataReader mySqlDataReader = command.ExecuteReader())
        {
          while (((DbDataReader) mySqlDataReader).Read())
            serversList.Add(new Servers()
            {
              gname = mySqlDataReader.GetString("gnam"),
              levelocation = mySqlDataReader.GetString("levellocation"),
              maxplaers = mySqlDataReader.GetInt64("pcap").ToString(),
              Gameid = mySqlDataReader.GetInt64("gid").ToString(),
              tipe = mySqlDataReader.GetString("type"),
              playersonline = mySqlDataReader.GetInt64("online").ToString(),
              Level = mySqlDataReader.GetString("mode")
            });
        }
      }
      return serversList;
    }

    public bool RecordAudit(string actorUserId, string targetUserId, string action, string reason)
    {
      try
      {
        using (var command = this.conection.CreateCommand())
        {
          command.CommandText = "INSERT INTO antihook_audit_events (actor_user_id, target_user_id, action_name, reason) VALUES (@actor, @target, @action, @reason);";
          command.Parameters.AddWithValue("@actor", String.IsNullOrWhiteSpace(actorUserId) ? (object)DBNull.Value : actorUserId);
          command.Parameters.AddWithValue("@target", String.IsNullOrWhiteSpace(targetUserId) ? (object)DBNull.Value : targetUserId);
          command.Parameters.AddWithValue("@action", action ?? "unknown");
          command.Parameters.AddWithValue("@reason", reason ?? "");
          command.ExecuteNonQuery();
          return true;
        }
      }
      catch { return false; }
    }

    public bool CreateBan(string userId, string hwidHash, string ipHash, string reason, string createdBy)
    {
      try
      {
        using (var command = this.conection.CreateCommand())
        {
          command.CommandText = "INSERT INTO antihook_bans (user_id, hwid_hash, ip_hash, reason, created_by) VALUES (@user, @hwid, @ip, @reason, @actor);";
          command.Parameters.AddWithValue("@user", String.IsNullOrWhiteSpace(userId) ? (object)DBNull.Value : userId);
          command.Parameters.AddWithValue("@hwid", String.IsNullOrWhiteSpace(hwidHash) ? (object)DBNull.Value : hwidHash);
          command.Parameters.AddWithValue("@ip", String.IsNullOrWhiteSpace(ipHash) ? (object)DBNull.Value : ipHash);
          command.Parameters.AddWithValue("@reason", reason ?? "");
          command.Parameters.AddWithValue("@actor", String.IsNullOrWhiteSpace(createdBy) ? (object)DBNull.Value : createdBy);
          command.ExecuteNonQuery();
          return true;
        }
      }
      catch { return false; }
    }
  }
}
