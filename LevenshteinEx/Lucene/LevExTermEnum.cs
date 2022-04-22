/* 
    * Licensed to the Apache Software Foundation (ASF) under one or more
    * contributor license agreements.  See the NOTICE file distributed with
    * this work for additional information regarding copyright ownership.
    * The ASF licenses this file to You under the Apache License, Version 2.0
    * (the "License"); you may not use this file except in compliance with
    * the License.  You may obtain a copy of the License at
    * 
    * http://www.apache.org/licenses/LICENSE-2.0
    * 
    * Unless required by applicable law or agreed to in writing, software
    * distributed under the License is distributed on an "AS IS" BASIS,
    * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    * See the License for the specific language governing permissions and
    * limitations under the License.
    */

using LevEx = LevenshteinEx.LevEx;

using IndexReader = Lucene.Net.Index.IndexReader;
using Term = Lucene.Net.Index.Term;

namespace Lucene.Net.Search
{

    /// <summary>Subclass of FilteredTermEnum for enumerating all terms that are similiar
    /// to the specified filter term.
    /// 
    /// <p/>Term enumerations are always ordered by Term.compareTo().  Each term in
    /// the enumeration is greater than all that precede it.
    /// </summary>
    public sealed class LevExTermEnum : FilteredTermEnum
    {
        readonly LevEx levEx;

        private float similarity;
        private bool endEnum = false;

        private bool isDisposed;

        private Term searchTerm = null;
        private System.String field;
        private System.String text;
        private System.String prefix;

        private float minimumSimilarity;
        private float scale_factor;

        /// <summary> Creates a FuzzyTermEnum with an empty prefix and a minSimilarity of 0.5f.
        /// <p/>
        /// After calling the constructor the enumeration is already pointing to the first 
        /// valid term if such a term exists. 
        /// 
        /// </summary>
        /// <param name="reader">
        /// </param>
        /// <param name="term">
        /// </param>
        /// <throws>  IOException </throws>
        /// <seealso cref="FuzzyTermEnum(IndexReader, Term, float, int)">
        /// </seealso>
        public LevExTermEnum(IndexReader reader, Term term) : this(reader, term, FuzzyQuery.defaultMinSimilarity, FuzzyQuery.defaultPrefixLength)
        {
        }

        /// <summary> Creates a FuzzyTermEnum with an empty prefix.
        /// <p/>
        /// After calling the constructor the enumeration is already pointing to the first 
        /// valid term if such a term exists. 
        /// 
        /// </summary>
        /// <param name="reader">
        /// </param>
        /// <param name="term">
        /// </param>
        /// <param name="minSimilarity">
        /// </param>
        /// <throws>  IOException </throws>
        /// <seealso cref="FuzzyTermEnum(IndexReader, Term, float, int)">
        /// </seealso>
        public LevExTermEnum(IndexReader reader, Term term, float minSimilarity) : this(reader, term, minSimilarity, FuzzyQuery.defaultPrefixLength)
        {
        }

        /// <summary> Constructor for enumeration of all terms from specified <c>reader</c> which share a prefix of
        /// length <c>prefixLength</c> with <c>term</c> and which have a fuzzy similarity &gt;
        /// <c>minSimilarity</c>.
        /// <p/>
        /// After calling the constructor the enumeration is already pointing to the first 
        /// valid term if such a term exists. 
        /// 
        /// </summary>
        /// <param name="reader">Delivers terms.
        /// </param>
        /// <param name="term">Pattern term.
        /// </param>
        /// <param name="minSimilarity">Minimum required similarity for terms from the reader. Default value is 0.5f.
        /// </param>
        /// <param name="prefixLength">Length of required common prefix. Default value is 0.
        /// </param>
        /// <throws>  IOException </throws>
        public LevExTermEnum(IndexReader reader, Term term, float minSimilarity, int prefixLength) : base()
        {

            if (minSimilarity >= 1.0f)
                throw new System.ArgumentException("minimumSimilarity cannot be greater than or equal to 1");
            else if (minSimilarity < 0.0f)
                throw new System.ArgumentException("minimumSimilarity cannot be less than 0");
            if (prefixLength < 0)
                throw new System.ArgumentException("prefixLength cannot be less than 0");

            this.minimumSimilarity = minSimilarity;
            this.scale_factor = 1.0f / (1.0f - minimumSimilarity);
            this.searchTerm = term;
            this.field = searchTerm.Field;

            //The prefix could be longer than the word.
            //It's kind of silly though.  It means we must match the entire word.
            int fullSearchTermLength = searchTerm.Text.Length;
            int realPrefixLength = prefixLength > fullSearchTermLength ? fullSearchTermLength : prefixLength;

            this.text = searchTerm.Text.Substring(realPrefixLength);
            this.prefix = searchTerm.Text.Substring(0, (realPrefixLength) - (0));

            levEx = new LevEx(term.Text, CalculateMaxDistance(term.Text.Length));

            SetEnum(reader.Terms(new Term(searchTerm.Field, prefix)));
        }

        /// <summary> The termCompare method in FuzzyTermEnum uses Levenshtein distance to 
        /// calculate the distance between the given term and the comparing term. 
        /// </summary>
        protected override bool TermCompare(Term term)
        {
            if ((System.Object)field == (System.Object)term.Field && term.Text.StartsWith(prefix))
            {
                System.String target = term.Text.Substring(prefix.Length);
                var matches = levEx.Matches(target);
                this.similarity = matches ? 1 : 0;
                return matches;
            }
            endEnum = true;
            return false;
        }

        public override float Difference()
        {
            return ((similarity - minimumSimilarity) * scale_factor);
        }

        public override bool EndEnum()
        {
            return endEnum;
        }


        /// <summary> The max Distance is the maximum Levenshtein distance for the text
        /// compared to some other value that results in score that is
        /// better than the minimum similarity.
        /// </summary>
        /// <param name="m">the length of the "other value"
        /// </param>
        /// <returns> the maximum levenshtein distance that we care about
        /// </returns>
        private int CalculateMaxDistance(int m)
        {
            return (int)((1 - minimumSimilarity) * (System.Math.Min(text.Length, m) + prefix.Length));
        }

        protected override void Dispose(bool disposing)
        {
            if (isDisposed) return;

            if (disposing)
            {
                searchTerm = null;
            }

            isDisposed = true;
            base.Dispose(disposing); //call super.close() and let the garbage collector do its work.
        }
    }
}