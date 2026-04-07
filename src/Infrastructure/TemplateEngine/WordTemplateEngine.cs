namespace NX_lims_Softlines_Command_System.src.Infrastructure.TemplateEngine
{
    /// <summary>
    /// Word 模板引擎
    /// 仅封装底层操作功能，不涉及业务逻辑
    /// </summary>
    public class WordTemplateEngine
    {

        /// <summary>
        /// 构造函数
        /// </summary>
        public WordTemplateEngine()
        {

        }

        /// <summary>
        /// 根据书签替换文本
        /// </summary>
        public void ReplaceText() 
        {
            //可能需要从数据库获取书签名

        }

        /// <summary>
        /// 用于处理页眉的文本替换
        /// </summary>
        public void ReplaceHeaderText()
        {

        }

        /// <summary>
        /// 用于处理页脚的文本替换
        /// </summary>
        public void ReplaceFootstepText()
        {

        }

        /// <summary>
        /// 图片替换书签位
        /// </summary>
        public void ReplaceWithImage()
        {

        }

        /// <summary>
        /// 对特定表格插入新行
        /// </summary>
        public void AddRow() 
        {

        }


        /// <summary>
        /// 对插入新表
        /// </summary>
        public void AddNewTable()
        {

        }

        /// <summary>
        /// 删除表格中的某一行
        /// </summary>
        public void RemoveRow()
        {
            //可能需要触发同一表格之中书签顺序的更新
        }


        //换页规则

        //表格合并规则

        //键入空白行


    }
}
