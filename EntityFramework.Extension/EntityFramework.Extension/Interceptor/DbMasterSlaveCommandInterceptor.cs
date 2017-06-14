﻿using System;
using System.Data.Common;
using System.Data.Entity.Infrastructure.Interception;
using Common.Logging;
using EntityFramework.Extension.Config;

namespace EntityFramework.Extension.Interceptor
{
    public class DbMasterSlaveCommandInterceptor : DbCommandInterceptor
    {
        const string TYPENAME = "DbMasterSlaveCommandInterceptor";

        public DbMasterSlaveCommandInterceptor()
        {
            LogManager.GetLogger(TYPENAME).Debug("DbMasterSlaveCommandInterceptor()");
        }

        //private string GetReadDbConnection()
        //{
        //    // 由于 存在多个从库的时候，存在心跳检测，权重匹配的问题。可用第三方负载均衡工具实现。
        //    // 从库没有的情况下，直接取主库
        //    return !string.IsNullOrEmpty(slavedbConn) ? slavedbConn : _masterIp;
        //}

        /// <summary>
        /// Linq 生成的select,insert + Database.SqlQuery<User>("select * from Users").ToList();
        /// prompt:在select语句中DbCommand.Transaction为null，而ef会为每个insert添加一个DbCommand.Transaction进行包裹
        /// </summary>
        /// <param name="command"></param>
        /// <param name="interceptionContext"></param>
        public override void ReaderExecuting(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
        {
            if (EntityFrameworkConfig.IsSlaveRead && !command.CommandText.StartsWith("insert", StringComparison.CurrentCultureIgnoreCase) && command.Transaction == null)
            {
                ChangeReadConn(command.Connection);
            }
            LogManager.GetLogger(TYPENAME).Debug(command.CommandText);
            base.ReaderExecuting(command, interceptionContext);
        }

        /// <summary>
        /// 修改读链接
        /// </summary>
        /// <param name="commandConnection"></param>
        private void ChangeReadConn(DbConnection commandConnection)
        {
            lock ("IsThreadSlave")
            {
                commandConnection.Close();
                commandConnection.ConnectionString = EntityFrameworkConfig.ReadConnstr;
                commandConnection.Open();
                #region Thread Connection 
                //if (EntityFrameworkConfig.IsThreadSlave && slaveDbConnection != null && !string.IsNullOrEmpty(slaveDbConnection.ConnectionString))
                //{
                //    if (slaveDbConnection.State == ConnectionState.Closed)
                //    {
                //        slaveDbConnection.Open();
                //    }
                //    commandConnection = slaveDbConnection;
                //}
                //else
                //{
                //    commandConnection.Close();
                //    commandConnection.ConnectionString = EntityFrameworkConfig.ReadConnstr;
                //    commandConnection.Open();
                //    if (EntityFrameworkConfig.IsThreadSlave && slaveDbConnection == null)
                //    {
                //        slaveDbConnection = commandConnection;
                //    }
                //} 
                #endregion
            }
        }

        /// <summary>
        /// Linq 生成的update,delete + Database.ExecuteSqlCommand
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <param name="interceptionContext"></param>
        public override void NonQueryExecuting(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {
            LogManager.GetLogger(TYPENAME).Debug(command.CommandText);
            base.NonQueryExecuting(command, interceptionContext);
        }

        /// <summary>
        /// 执行sql语句，并返回第一行第一列，没有找到返回null,如果数据库中值为null，则返回 DBNull.Value
        /// </summary>
        /// <param name="command"></param>
        /// <param name="interceptionContext"></param>
        public override void ScalarExecuting(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {
            LogManager.GetLogger(TYPENAME).Debug(command.CommandText);
            base.ScalarExecuting(command, interceptionContext);
        }

        public override void ReaderExecuted(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
        {
            base.ReaderExecuted(command, interceptionContext);
        }

        public override void NonQueryExecuted(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {
            base.NonQueryExecuted(command, interceptionContext);
        }

        public override void ScalarExecuted(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {
            base.ScalarExecuted(command, interceptionContext);
        }
    }

}