using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UmbracoAutomation.WebApi
{
    public class PropertyValueRule
    {
        public UmbracoDocumentBy By { get; set; }
        public string Selector { get; set; }
        public string Name { get; set; }
        public string Value
        {
            get => _value;
            set
            {
                _valueList = value.Split(',').Select(s=>s.Trim()).ToList();
                _value = value;
            }
        }
        private List<string> _valueList;
        private string _value;
        public List<string> GetValues() => _valueList;
        public IList<T> GetSelectors<T>() where T : struct
        {
            return typeof(T) == typeof(int)
                ? (IList<T>)Selector.Split(',').Select(s => int.Parse(s.Trim())).ToList()
                : (IList<T>)Selector.Split(',').Select(s => s.Trim()).ToList();
        }
        public List<string> GetSelectors()
        {
            return Selector.Split(',').Select(s => s.Trim()).ToList();
        }
    }
}
