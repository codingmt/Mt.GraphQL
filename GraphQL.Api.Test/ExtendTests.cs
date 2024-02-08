using Mt.GraphQL.Internal;

namespace Mt.GraphQL.Api.Test
{
    public class ExtendTests
    {
        [Test]
        public void TestValidExtends()
        {
            var extends = Extend.Parse(null);
            Assert.That(extends, Is.Not.Null);
            Assert.That(extends, Has.Length.EqualTo(0));

            extends = Extend.Parse("aaa,bbb");
            Assert.That(extends, Has.Length.EqualTo(2));
            Assert.That(extends[0].ToString(), Is.EqualTo("aaa"));
            Assert.That(extends[0].Name, Is.EqualTo("aaa"));
            Assert.That(extends[0].Properties, Is.Null);
            Assert.That(extends[1].ToString(), Is.EqualTo("bbb"));
            Assert.That(extends[1].Name, Is.EqualTo("bbb"));
            Assert.That(extends[1].Properties, Is.Null);

            extends = Extend.Parse("aaa(bbb,ccc(ddd,eee),fff(ggg))");
            Assert.That(extends, Has.Length.EqualTo(1));
            Assert.That(extends[0].ToString(), Is.EqualTo("aaa(bbb,ccc(ddd,eee),fff(ggg))"));
            Assert.That(extends[0].Name, Is.EqualTo("aaa"));
            Assert.That(extends[0].Properties, Is.Not.Null);
            Assert.That(extends[0].Properties, Has.Length.EqualTo(3));
            Assert.That(extends[0].Properties[0].ToString(), Is.EqualTo("bbb"));
            Assert.That(extends[0].Properties[0].Name, Is.EqualTo("bbb"));
            Assert.That(extends[0].Properties[0].Properties, Is.Null);
            Assert.That(extends[0].Properties[1].ToString(), Is.EqualTo("ccc(ddd,eee)"));
            Assert.That(extends[0].Properties[2].ToString(), Is.EqualTo("fff(ggg)"));
        }

        [Test]
        public void TestInvalidExtends()
        {
            var extends = Extend.Parse("aaa,");
            Assert.That(extends, Is.Not.Null);
            Assert.That(extends, Has.Length.EqualTo(1));
            Assert.That(extends[0].ToString(), Is.EqualTo("aaa"));

            Assert.Throws<InternalException>(() => Extend.Parse("aaa,bbb.ccc"), "Extension is invalid: bbb.ccc");
            
            Assert.Throws<InternalException>(() => Extend.Parse("aaa()"), "No field name found on position 4 in extend aaa()");

            extends = Extend.Parse("aaa(bbb");
            Assert.That(extends, Is.Not.Null);
            Assert.That(extends, Has.Length.EqualTo(1));
            Assert.That(extends[0].ToString(), Is.EqualTo("aaa(bbb)"));

            extends = Extend.Parse("aaa(bbb,");
            Assert.That(extends, Is.Not.Null);
            Assert.That(extends, Has.Length.EqualTo(1));
            Assert.That(extends[0].ToString(), Is.EqualTo("aaa(bbb)"));
        }
    }
}
