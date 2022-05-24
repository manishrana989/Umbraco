exports = async (source) => {
    const { referencedMedia } = source;

    if (referencedMedia && referencedMedia.length > 0) {
        const cluster = context.services.get("mongodb-atlas");
        const mediaColl = cluster.db("PRXXXX").collection("Media");

        const result = [];
        for (var i = 0; i < referencedMedia.length; i++) {
            const media = await mediaColl.findOne({ "_id": referencedMedia[i] });
            if (media) {
                result.push(media);
            }
        }

        return result;
    }

    return null;

};