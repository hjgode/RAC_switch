//pulled directly from Mono - MIT X11 license

// XmlCanonicalizer.cs - C14N implementation for XML Signature
// http://www.w3.org/TR/xml-c14n
//
// Author:
//	Aleksey Sanin (aleksey@aleksey.com)
//
// (C) 2003 Aleksey Sanin (aleksey@aleksey.com)
//

using System.Collections;
using System.IO;
using System.Text;
using System.Xml;

namespace OpenNETCF.Web.Services2  { 

	//internal class XmlCanonicalizer 
	public class XmlCanonicalizer 
	{

		private enum XmlCanonicalizerState
		{
			BeforeDocElement,
			InsideDocElement,
			AfterDocElement
		}
		
		// c14n parameters
		private bool comments;
		private bool exclusive;

		// input/output
		private XmlNodeList xnl;
		private StringBuilder res;
		
		// namespaces rendering stack
		private XmlCanonicalizerState state;
		private ArrayList visibleNamespaces;
		private int prevVisibleNamespacesStart;
		private int prevVisibleNamespacesEnd;

		public XmlCanonicalizer (bool withComments, bool excC14N)
		{	    
			res = new StringBuilder ();
			comments = withComments;
			exclusive = excC14N;
			state = XmlCanonicalizerState.BeforeDocElement;
			visibleNamespaces = new ArrayList ();
			prevVisibleNamespacesStart = 0;
			prevVisibleNamespacesEnd = 0;
		}
		
		public Stream Canonicalize (XmlDocument doc)
		{
			WriteDocumentNode (doc);
			
			UTF8Encoding utf8 = new UTF8Encoding ();
			byte[] data = utf8.GetBytes (res.ToString ());
			return new MemoryStream (data);
		}
		
		//interface mod
		public Stream Canonicalize (XmlNode node)
		{
			//WriteDocumentNode (doc);
			state = XmlCanonicalizerState.BeforeDocElement;
			//for (XmlNode child = node.FirstChild; child != null; child = child.NextSibling)
			//	WriteNode (child);
			WriteNode (node);
			
			UTF8Encoding utf8 = new UTF8Encoding ();
			byte[] data = utf8.GetBytes (res.ToString ());
			return new MemoryStream (data);
		}

		public Stream Canonicalize (XmlNodeList nodes)
		{
			xnl = nodes;
			if (nodes == null || nodes.Count < 1)
			    return null;
			return Canonicalize (nodes[0].OwnerDocument);
		}		

		private void WriteNode (XmlNode node)
		{
			// Console.WriteLine ("C14N Debug: node=" + node.Name);

			bool visible = IsNodeVisible (node);
			switch (node.NodeType) {
			case XmlNodeType.Document:
			case XmlNodeType.DocumentFragment:
				WriteDocumentNode (node);
				break;
			case XmlNodeType.Element:
				WriteElementNode (node, visible);
				break;
			case XmlNodeType.CDATA:
			case XmlNodeType.SignificantWhitespace:
			case XmlNodeType.Text:
        			// CDATA sections are processed as text nodes
				WriteTextNode (node, visible);
				break;
			case XmlNodeType.Whitespace:
				if (state == XmlCanonicalizerState.InsideDocElement)
					WriteTextNode (node, visible);
				break;
			case XmlNodeType.Comment:
				WriteCommentNode (node, visible);
				break;
			case XmlNodeType.ProcessingInstruction:
				WriteProcessingInstructionNode (node, visible);
				break;
			case XmlNodeType.EntityReference:
				throw new XmlException ("Entity references should be resolved by parser", null);
			case XmlNodeType.Attribute:
				throw new XmlException ("Attribute node is impossible here", null);
			case XmlNodeType.EndElement:
				throw new XmlException ("EndElement node is impossible here", null);
			case XmlNodeType.EndEntity:
				throw new XmlException ("EndEntity node is impossible here", null);
			case XmlNodeType.DocumentType:
			case XmlNodeType.Entity:
			case XmlNodeType.Notation:
			case XmlNodeType.XmlDeclaration:
				// just do nothing
				break;
			}
		}

		private void WriteDocumentNode (XmlNode node)
    	{
			state = XmlCanonicalizerState.BeforeDocElement;
			for (XmlNode child = node.FirstChild; child != null; child = child.NextSibling)
				WriteNode (child);
		}
		
		// Element Nodes
		// If the element is not in the node-set, then the result is obtained 
		// by processing the namespace axis, then the attribute axis, then 
		// processing the child nodes of the element that are in the node-set 
		// (in document order). If the element is inthe node-set, then the result 
		// is an open angle bracket (<), the element QName, the result of 
		// processing the namespace axis, the result of processing the attribute 
		// axis, a close angle bracket (>), the result of processing the child 
		// nodes of the element that are in the node-set (in document order), an 
		// open angle bracket, a forward slash (/), the element QName, and a close 
		// angle bracket.
		private void WriteElementNode (XmlNode node, bool visible)
		{
			// Console.WriteLine ("Debug: element node");
		    
			// remember current state 
			int savedPrevVisibleNamespacesStart = prevVisibleNamespacesStart;
			int savedPrevVisibleNamespacesEnd = prevVisibleNamespacesEnd;
			int savedVisibleNamespacesSize = visibleNamespaces.Count;
			XmlCanonicalizerState s = state;
			if (visible && state == XmlCanonicalizerState.BeforeDocElement)
				state = XmlCanonicalizerState.InsideDocElement;
		    
			// write start tag
			if (visible) {
				res.Append ("<");
				res.Append (node.Name);
			}
		    
			// this is odd but you can select namespaces
			// and attributes even if node itself is not visible
			WriteNamespacesAxis (node, visible);
			WriteAttributesAxis (node);			
	
			if (visible)
				res.Append (">");

			// write children
			for (XmlNode child = node.FirstChild; child != null; child = child.NextSibling)
				WriteNode (child);
		    		    
			// write end tag	    
			if (visible) {
				res.Append ("</");
				res.Append (node.Name);
				res.Append (">");
			}
		    
			// restore state
			if (visible && s == XmlCanonicalizerState.BeforeDocElement)
				state = XmlCanonicalizerState.AfterDocElement;
			prevVisibleNamespacesStart = savedPrevVisibleNamespacesStart;
			prevVisibleNamespacesEnd = savedPrevVisibleNamespacesEnd;
			if (visibleNamespaces.Count > savedVisibleNamespacesSize) {
				visibleNamespaces.RemoveRange (savedVisibleNamespacesSize, 
					visibleNamespaces.Count - savedVisibleNamespacesSize);
			}
		}

		// Namespace Axis
		// Consider a list L containing only namespace nodes in the 
		// axis and in the node-set in lexicographic order (ascending). To begin 
		// processing L, if the first node is not the default namespace node (a node 
		// with no namespace URI and no local name), then generate a space followed 
		// by xmlns="" if and only if the following conditions are met:
		//    - the element E that owns the axis is in the node-set
		//    - The nearest ancestor element of E in the node-set has a default 
		//	    namespace node in the node-set (default namespace nodes always 
		//      have non-empty values in XPath)
		// The latter condition eliminates unnecessary occurrences of xmlns="" in 
		// the canonical form since an element only receives an xmlns="" if its 
		// default namespace is empty and if it has an immediate parent in the 
		// canonical form that has a non-empty default namespace. To finish 
		// processing  L, simply process every namespace node in L, except omit 
		// namespace node with local name xml, which defines the xml prefix, 
		// if its string value is http://www.w3.org/XML/1998/namespace.
		private void WriteNamespacesAxis (XmlNode node, bool visible)
		{
			// Console.WriteLine ("Debug: namespaces");

			XmlDocument doc = node.OwnerDocument;    
			bool has_empty_namespace = false;
			ArrayList list = new ArrayList ();
			for (XmlNode cur = node; cur != null && cur != doc; cur = cur.ParentNode) {
			        foreach (XmlNode attribute in cur.Attributes) {		
					if (!IsNamespaceNode (attribute)) 
			    			continue;
			    	
					// get namespace prefix
					string prefix = string.Empty;
					if (attribute.Prefix == "xmlns") 
						prefix = attribute.LocalName;
			    
				        // check if it is "xml" namespace			    
					if (prefix == "xml" && attribute.Value == "http://www.w3.org/XML/1998/namespace")
						continue;
			    
					// make sure that this is an active namespace
					// for our node
					string ns = node.GetNamespaceOfPrefix (prefix);
					if (ns != attribute.Value) 
						continue;
			    
					// check that it is selected with XPath
					if (!IsNodeVisible (attribute)) 
						continue;

					// check that we have not rendered it yet
					bool rendered = IsNamespaceRendered (prefix, attribute.Value);

					// add to the visible namespaces stack
					if (visible)
						visibleNamespaces.Add (attribute);			      
			    
					if (!rendered)
						list.Add (attribute);
				    
					if (prefix == string.Empty)
						has_empty_namespace = true;
				}
			}

			// add empty namespace if needed		    
			if (visible && !has_empty_namespace && !IsNamespaceRendered (string.Empty, string.Empty)) 
				res.Append (" xmlns=\"\"");
		    
			list.Sort (new XmlDsigC14NTransformNamespacesComparer ());
			foreach (object obj in list) {
				XmlNode attribute = (obj as XmlNode);
				if (attribute != null) {
					res.Append (" ");
					res.Append (attribute.Name);
					res.Append ("=\"");
					res.Append (attribute.Value);
					res.Append ("\"");
				}
			}
		    
			// move the rendered namespaces stack
			if (visible) {
				prevVisibleNamespacesStart = prevVisibleNamespacesEnd;
				prevVisibleNamespacesEnd = visibleNamespaces.Count;	
			}
		}
		
		// Attribute Axis 
	    	// In lexicographic order (ascending), process each node that 
		// is in the element's attribute axis and in the node-set.
		// 
		// The processing of an element node E MUST be modified slightly 
		// when an XPath node-set is given as input and the element's 
		// parent is omitted from the node-set.
		private void WriteAttributesAxis (XmlNode node)
		{
			// Console.WriteLine ("Debug: attributes");
		
			ArrayList list = new ArrayList ();
			foreach (XmlNode attribute in node.Attributes) {	
				if (!IsNamespaceNode (attribute) && IsNodeVisible (attribute))
					list.Add (attribute);
			}
		     
		        // Add attributes from "xml" namespace for "inclusive" c14n only:
			//
			// The method for processing the attribute axis of an element E 
		        // in the node-set is enhanced. All element nodes along E's 
			// ancestor axis are examined for nearest occurrences of 
			// attributes in the xml namespace, such as xml:lang and 
			// xml:space (whether or not they are in the node-set). 
			// From this list of attributes, remove any that are in E's 
			// attribute axis (whether or not they are in the node-set). 
			// Then, lexicographically merge this attribute list with the 
			// nodes of E's attribute axis that are in the node-set. The 
			// result of visiting the attribute axis is computed by 
			// processing the attribute nodes in this merged attribute list.
			if (!exclusive && node.ParentNode != null && !IsNodeVisible (node.ParentNode.ParentNode)) {
	    			// if we have whole document then the node.ParentNode.ParentNode
				// is always visible
				for (XmlNode cur = node.ParentNode; cur != null; cur = cur.ParentNode) {
					foreach (XmlNode attribute in cur.Attributes) {
						// we are looking for "xml:*" attributes
						if (attribute.Prefix != "xml")
							continue;
				
						// exclude ones that are in the node's attributes axis
						if (node.Attributes.GetNamedItem (attribute.LocalName, attribute.NamespaceURI) != null)
							continue;
				
						// finally check that we don't have the same attribute in our list
						bool found = false;
						foreach (object obj in list) {
							XmlNode n = (obj as XmlNode);
							if (n.Prefix == "xml" && n.LocalName == attribute.LocalName) {
								found = true;
								break;
							}
						}
				
						if (found) 
							continue;
				
						// now we can add this attribute to our list
						list.Add (attribute);
					}
				}		
			}
		    	
			// sort namespaces and write results	    
			list.Sort (new XmlDsigC14NTransformAttributesComparer ());
			foreach (object obj in list) {
				XmlNode attribute = (obj as XmlNode);
				if (attribute != null) {
					res.Append (" ");
					res.Append (attribute.Name);
					res.Append ("=\"");
					res.Append (NormalizeString (attribute.Value, XmlNodeType.Attribute));
					res.Append ("\"");
				}
			}
		}

            	// Text Nodes
            	// the string value, except all ampersands are replaced 
            	// by &amp;, all open angle brackets (<) are replaced by &lt;, all closing 
            	// angle brackets (>) are replaced by &gt;, and all #xD characters are 
            	// replaced by &#xD;.
		private void WriteTextNode (XmlNode node, bool visible)
		{
			// Console.WriteLine ("Debug: text node");
			if (visible)
				res.Append (NormalizeString (node.Value, XmlNodeType.Text));
		}		

		// Comment Nodes
            	// Nothing if generating canonical XML without comments. For 
            	// canonical XML with comments, generate the opening comment 
            	// symbol (<!--), the string value of the node, and the 
            	// closing comment symbol (-->). Also, a trailing #xA is rendered 
	        // after the closing comment symbol for comment children of the 
        	// root node with a lesser document order than the document 
            	// element, and a leading #xA is rendered before the opening 
            	// comment symbol of comment children of the root node with a 
            	// greater document order than the document element. (Comment 
	        // children of the root node represent comments outside of the 
            	// top-level document element and outside of the document type 
            	// declaration).
		private void WriteCommentNode (XmlNode node, bool visible)
		{
			// Console.WriteLine ("Debug: comment node");
			if (visible && comments) {
			    if (state == XmlCanonicalizerState.AfterDocElement)
				    res.Append ("\x0A<!--");
			    else
				    res.Append ("<!--");
			
			    res.Append (NormalizeString (node.Value, XmlNodeType.Comment));
			    
			    if (state == XmlCanonicalizerState.BeforeDocElement)
				    res.Append ("-->\x0A");
			    else
				    res.Append ("-->");
	    		}
		}
		
        	// Processing Instruction (PI) Nodes- 
            	// The opening PI symbol (<?), the PI target name of the node, 
            	// a leading space and the string value if it is not empty, and 
            	// the closing PI symbol (?>). If the string value is empty, 
            	// then the leading space is not added. Also, a trailing #xA is 
            	// rendered after the closing PI symbol for PI children of the 
            	// root node with a lesser document order than the document 
            	// element, and a leading #xA is rendered before the opening PI 
            	// symbol of PI children of the root node with a greater document 
            	// order than the document element.
		private void WriteProcessingInstructionNode (XmlNode node, bool visible)
		{
			// Console.WriteLine ("Debug: PI node");

			if (visible) {
				if (state == XmlCanonicalizerState.AfterDocElement)
					res.Append ("\x0A<?");
				else
					res.Append ("<?");
			
				res.Append (node.Name);
				if (node.Value.Length > 0) {
					res.Append (" ");
					res.Append (NormalizeString (node.Value, XmlNodeType.ProcessingInstruction));
				}
			
				if (state == XmlCanonicalizerState.BeforeDocElement)
					res.Append ("?>\x0A");
				else
					res.Append ("?>");
	    		}
		}
		
		private bool IsNodeVisible (XmlNode node)
		{
			// if node list is empty then we process whole document
			if (xnl == null) 
				return true;
		    
			// walk thru the list
			foreach (XmlNode xn in xnl) {
				if (node.Equals (xn)) 
					return true;
			}
		    
			return false;
		}

		private bool IsNamespaceRendered (string prefix, string uri)
		{
			// if the default namespace xmlns="" is not re-defined yet
			// then we do not want to print it out
			bool IsEmptyNs = prefix == string.Empty && uri == string.Empty;
			int start = (IsEmptyNs) ? 0 : prevVisibleNamespacesStart;
			for (int i = visibleNamespaces.Count - 1; i >= start; i--) {
				XmlNode node = (visibleNamespaces[i] as XmlNode);
				if (node != null) {
					// get namespace prefix
					string p = string.Empty;
					if (node.Prefix == "xmlns") 
						p = node.LocalName;
					if (p == prefix) 
						return node.Value == uri;
				}
			}
		    
			return IsEmptyNs;
		}
		
		private bool IsNamespaceNode (XmlNode node)
    		{
			if (node == null || node.NodeType != XmlNodeType.Attribute) 
				return false;
			return node.NamespaceURI == "http://www.w3.org/2000/xmlns/";
		}
    
		private string NormalizeString (string input, XmlNodeType type)
		{
			StringBuilder sb = new StringBuilder ();
			for (int i = 0; i < input.Length; i++) {
				char ch = input[i];
				if (ch == '<' && (type == XmlNodeType.Attribute || type == XmlNodeType.Text))
					sb.Append ("&lt;");
				else if (ch == '>' && type == XmlNodeType.Text)
					sb.Append ("&gt;");
				else if (ch == '&' && (type == XmlNodeType.Attribute || type == XmlNodeType.Text))
					sb.Append ("&amp;");
				else if (ch == '\"' && type == XmlNodeType.Attribute)
					sb.Append ("&quot;");
				else if (ch == '\x09' && type == XmlNodeType.Attribute)
					sb.Append ("&#x9;");
				else if (ch == '\x0A' && type == XmlNodeType.Attribute)
					sb.Append ("&#xA;");
				else if (ch == '\x0D' && (type == XmlNodeType.Attribute ||
							  type == XmlNodeType.Text ||
							  type == XmlNodeType.Comment ||
							  type == XmlNodeType.ProcessingInstruction))
					sb.Append ("&#xD;");
				else
					sb.Append (ch);
			}
		    
			return sb.ToString ();
		}
	}
    
	internal class XmlDsigC14NTransformAttributesComparer : IComparer
	{
		public int Compare (object x, object y)
		{
			XmlNode n1 = (x as XmlNode);
			XmlNode n2 = (y as XmlNode);
		
			// simple cases
			if (n1 == n2) 
				return 0;
			else if (n1 == null) 
				return -1;
			else if (n2 == null) 
				return 1;
	    		else if (n1.Prefix == n2.Prefix) 
				return string.Compare (n1.LocalName, n2.LocalName);
	
    			// Attributes in the default namespace are first
			// because the default namespace is not applied to
    			// unqualified attributes
			if (n1.Prefix == string.Empty) 
				return -1;
			else if (n2.Prefix == string.Empty) 
				return 1;
		    
	    		int ret = string.Compare (n1.NamespaceURI, n2.NamespaceURI);
			if (ret == 0)
				ret = string.Compare (n1.LocalName, n2.LocalName);
			return ret;
		}
	}

	internal class XmlDsigC14NTransformNamespacesComparer : IComparer
	{
		public int Compare (object x, object y)
		{
			XmlNode n1 = (x as XmlNode);
			XmlNode n2 = (y as XmlNode);
		
			// simple cases
			if (n1 == n2) 
				return 0;
			else if (n1 == null) 
				return -1;
			else if (n2 == null) 
				return 1;
			else if (n1.Prefix == string.Empty) 
				return -1;
			else if (n2.Prefix == string.Empty) 
				return 1;
		    
			return string.Compare (n1.LocalName, n2.LocalName);
		}
	}
}

